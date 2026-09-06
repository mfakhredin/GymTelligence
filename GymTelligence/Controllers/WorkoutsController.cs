using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Controllers;

[ApiController, Authorize, Route("api/workouts")]
public sealed class WorkoutsController(IWorkoutService service) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<PlanSummary>>> Plans(CancellationToken ct) => Ok(await service.GetPlansAsync(ct));

    [HttpGet("plans/{id:int}")]
    public async Task<ActionResult<PlanDetailResponse>> Plan(int id, CancellationToken ct) => Ok(await service.GetPlanAsync(id, ct));

    [HttpPost("plans/generate")]
    public async Task<ActionResult<PlanDetailResponse>> Generate(CancellationToken ct) => Ok(await service.GenerateAsync(ct));

    [HttpPost("sets/{id:int}/toggle")]
    public async Task<ActionResult<object>> ToggleSet(int id, CancellationToken ct) => Ok(new { isCompleted = await service.ToggleSetAsync(id, ct) });

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, WorkoutFeedbackRequest request, CancellationToken ct)
    {
        await service.CompleteWorkoutAsync(id, request, ct);
        return NoContent();
    }
}
