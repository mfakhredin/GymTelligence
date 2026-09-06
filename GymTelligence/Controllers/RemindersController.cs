using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Controllers;

[ApiController, Authorize, Route("api/reminders")]
public sealed class RemindersController(IReminderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReminderResponse>>> Get(CancellationToken ct) => Ok(await service.GetAsync(ct));

    [HttpPost]
    public async Task<ActionResult<ReminderResponse>> Create(SaveReminderRequest request, CancellationToken ct) => Ok(await service.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReminderResponse>> Update(int id, SaveReminderRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
