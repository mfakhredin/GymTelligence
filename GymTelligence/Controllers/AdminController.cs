using Application.Models;
using Application.Services;
using Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Controllers;

[ApiController, Authorize(Roles = "Trainer,Admin"), Route("api/staff")]
public sealed class AdminController(IAdminService service, IBackupService backups) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<StaffOverviewResponse>> Overview(CancellationToken ct) => Ok(await service.OverviewAsync(ct));

    [HttpGet("users"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> Users(CancellationToken ct) => Ok(await service.UsersAsync(ct));

    [HttpPut("users/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserAccessRequest request, CancellationToken ct)
    {
        await service.UpdateUserAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("templates"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<PlanSummary>>> Templates(CancellationToken ct) => Ok(await service.TemplatesAsync(ct));

    [HttpPost("templates"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlanSummary>> CreateTemplate(SaveWorkoutTemplateRequest request, CancellationToken ct)
    {
        var result = await service.CreateTemplateAsync(request, ct);
        return CreatedAtAction(nameof(Templates), new { id = result.Id }, result);
    }

    [HttpPut("templates/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PlanSummary>> UpdateTemplate(int id, SaveWorkoutTemplateRequest request, CancellationToken ct) =>
        Ok(await service.UpdateTemplateAsync(id, request, ct));

    [HttpDelete("templates/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTemplate(int id, CancellationToken ct)
    {
        await service.DeleteTemplateAsync(id, ct);
        return NoContent();
    }

    [HttpGet("backups"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<BackupResponse>>> Backups(CancellationToken ct) => Ok(await backups.RecentAsync(ct));

    [HttpPost("backups"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<BackupResponse>> CreateBackup(CancellationToken ct) =>
        Ok(await backups.CreateAsync(ct));
}
