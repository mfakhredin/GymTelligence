using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Controllers;

[ApiController, Authorize, Route("api/progress")]
public sealed class ProgressController(IProgressService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProgressResponse>> Get(CancellationToken ct) => Ok(await service.GetAsync(ct));

    [HttpPost]
    public async Task<ActionResult<ProgressEntryResponse>> Add(AddProgressRequest request, CancellationToken ct) => Ok(await service.AddAsync(request, ct));
}
