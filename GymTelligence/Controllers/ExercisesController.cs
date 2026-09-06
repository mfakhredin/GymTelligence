using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Controllers;

[ApiController, Authorize, Route("api/exercises")]
public sealed class ExercisesController(IExerciseService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExerciseResponse>>> Get([FromQuery] string? search, [FromQuery] string? muscle,
        [FromQuery] string? equipment, [FromQuery] string? difficulty, CancellationToken ct) =>
        Ok(await service.GetAsync(search, muscle, equipment, difficulty, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExerciseResponse>> GetById(int id, CancellationToken ct) => Ok(await service.GetByIdAsync(id, ct));

    [HttpPost, Authorize(Roles = "Trainer,Admin")]
    public async Task<ActionResult<ExerciseResponse>> Create(SaveExerciseRequest request, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Trainer,Admin")]
    public async Task<ActionResult<ExerciseResponse>> Update(int id, SaveExerciseRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}"), Authorize(Roles = "Trainer,Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
