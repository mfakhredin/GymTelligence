using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common;
using Application.Models;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class StripeSubscriptionService(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IHttpClientFactory clientFactory,
    IConfiguration configuration) : ISubscriptionService
{
    public async Task<SubscriptionResponse> StatusAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
        return new SubscriptionResponse(user.SubscriptionStatus.ToString(), user.CurrentPeriodEnd, IsCheckoutConfigured());
    }

    public async Task<CheckoutResponse> CreateCheckoutAsync(string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        var secret = configuration["Stripe:SecretKey"];
        var price = configuration["Stripe:PriceId"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(price))
            throw new AppValidationException("Stripe checkout has not been configured.");
        var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
        var values = new Dictionary<string, string>
        {
            ["mode"] = "subscription", ["client_reference_id"] = user.Id.ToString(),
            ["customer_email"] = user.Email, ["line_items[0][price]"] = price,
            ["line_items[0][quantity]"] = "1", ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl, ["allow_promotion_codes"] = "true",
            ["subscription_data[metadata][user_id]"] = user.Id.ToString()
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(values)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        var response = await clientFactory.CreateClient().SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new AppValidationException("Stripe could not start checkout. Check the configured keys and price.");
        using var json = JsonDocument.Parse(content);
        var url = json.RootElement.GetProperty("url").GetString();
        return new CheckoutResponse(url ?? throw new AppValidationException("Stripe did not return a checkout URL."));
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        var secret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret) || !VerifySignature(payload, signature, secret))
            throw new AppForbiddenException("Invalid Stripe webhook signature.");
        using var json = JsonDocument.Parse(payload);
        var root = json.RootElement;
        var eventType = root.GetProperty("type").GetString() ?? string.Empty;
        var data = root.GetProperty("data").GetProperty("object");

        int? userId = null;
        if (eventType == "checkout.session.completed" &&
            data.TryGetProperty("client_reference_id", out var reference) &&
            int.TryParse(reference.GetString(), out var checkoutUserId))
            userId = checkoutUserId;
        else if (data.TryGetProperty("metadata", out var metadata) &&
                 metadata.TryGetProperty("user_id", out var metadataUser) &&
                 int.TryParse(metadataUser.GetString(), out var metadataUserId))
            userId = metadataUserId;

        var customerId = StringValue(data, "customer");
        var subscriptionId = eventType.StartsWith("customer.subscription.", StringComparison.Ordinal)
            ? StringValue(data, "id")
            : StringValue(data, "subscription");
        var user = userId.HasValue
            ? await db.Users.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken)
            : await db.Users.SingleOrDefaultAsync(x =>
                (customerId != null && x.StripeCustomerId == customerId) ||
                (subscriptionId != null && x.StripeSubscriptionId == subscriptionId), cancellationToken);
        if (user is null) return;

        var status = eventType switch
        {
            "checkout.session.completed" => SubscriptionStatus.Active,
            "customer.subscription.deleted" => SubscriptionStatus.Canceled,
            "invoice.payment_failed" => SubscriptionStatus.PastDue,
            "customer.subscription.created" or "customer.subscription.updated" => MapStatus(StringValue(data, "status")),
            _ => (SubscriptionStatus?)null
        };
        if (status is null) return;

        user.StripeCustomerId = customerId ?? user.StripeCustomerId;
        user.StripeSubscriptionId = subscriptionId ?? user.StripeSubscriptionId;
        user.SubscriptionStatus = status.Value;
        if (data.TryGetProperty("current_period_end", out var period) && period.TryGetInt64(out var unixPeriod))
            user.CurrentPeriodEnd = DateTimeOffset.FromUnixTimeSeconds(unixPeriod).UtcDateTime;

        var record = await db.Subscriptions.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        if (record is null)
        {
            record = new Subscription { UserId = user.Id };
            db.Subscriptions.Add(record);
        }
        record.StripeCustomerId = user.StripeCustomerId;
        record.StripeSubscriptionId = user.StripeSubscriptionId;
        record.Status = status.Value;
        record.CurrentPeriodEnd = user.CurrentPeriodEnd;
        await db.SaveChangesAsync(cancellationToken);
    }

    private bool IsCheckoutConfigured() =>
        !string.IsNullOrWhiteSpace(configuration["Stripe:SecretKey"]) &&
        !string.IsNullOrWhiteSpace(configuration["Stripe:PriceId"]);

    private static bool VerifySignature(string payload, string header, string secret)
    {
        var parts = header.Split(',').Select(x => x.Split('=', 2)).Where(x => x.Length == 2)
            .ToDictionary(x => x[0], x => x[1]);
        if (!parts.TryGetValue("t", out var timestamp) || !parts.TryGetValue("v1", out var expected) ||
            !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var unix) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unix) > 300) return false;
        try
        {
            var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
            return CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(expected));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? StringValue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static SubscriptionStatus MapStatus(string? value) => value switch
    {
        "active" => SubscriptionStatus.Active,
        "trialing" => SubscriptionStatus.Trialing,
        "past_due" or "unpaid" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Canceled,
        "incomplete" or "incomplete_expired" => SubscriptionStatus.Incomplete,
        _ => SubscriptionStatus.Free
    };
}
