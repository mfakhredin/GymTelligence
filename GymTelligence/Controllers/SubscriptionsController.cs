using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Controllers;

[ApiController, Route("api/subscriptions")]
public sealed class SubscriptionsController(ISubscriptionService service) : ControllerBase
{
    [HttpGet("status"), Authorize]
    public async Task<ActionResult<SubscriptionResponse>> Status(CancellationToken ct) => Ok(await service.StatusAsync(ct));

    [HttpPost("checkout"), Authorize]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CancellationToken ct)
    {
        var root = $"{Request.Scheme}://{Request.Host}";
        return Ok(await service.CreateCheckoutAsync($"{root}/subscription?result=success", $"{root}/subscription?result=cancel", ct));
    }

    [HttpPost("webhook"), AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        await service.HandleWebhookAsync(payload, Request.Headers["Stripe-Signature"].ToString(), ct);
        return Ok();
    }
}
