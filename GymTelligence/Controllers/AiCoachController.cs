using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymTelligence.Controllers;

[ApiController, Authorize, Route("api/coach")]
public sealed class AiCoachController(IAiCoachService service) : ControllerBase
{
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<AiMessageResponse>>> History(CancellationToken ct) => Ok(await service.HistoryAsync(ct));

    [HttpPost("ask"), EnableRateLimiting("ai")]
    public async Task<ActionResult<AskCoachResponse>> Ask(AskCoachRequest request, CancellationToken ct) => Ok(await service.AskAsync(request, ct));
}
