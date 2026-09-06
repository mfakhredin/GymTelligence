using System.Net;
using System.Net.Mail;
using Application.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(configuration["Email:FromAddress"]);

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return;

        var host = configuration["Email:SmtpHost"]!;
        var port = int.TryParse(configuration["Email:SmtpPort"], out var configuredPort) ? configuredPort : 587;
        var fromAddress = configuration["Email:FromAddress"]!;
        var fromName = configuration["Email:FromName"] ?? "GymTelligence";
        var username = configuration["Email:Username"];
        var password = configuration["Email:Password"];

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = "Reset your GymTelligence password",
            Body = $"Hello {recipientName},\n\nUse this single-use link to reset your GymTelligence password:\n{resetUrl}\n\nThe link expires in one hour. If you did not request it, you can ignore this email.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !bool.TryParse(configuration["Email:EnableSsl"], out var enableSsl) || enableSsl,
            UseDefaultCredentials = false,
            Credentials = string.IsNullOrWhiteSpace(username) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(username, password)
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}
