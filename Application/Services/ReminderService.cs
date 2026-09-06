using Application.Common;
using Application.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class ReminderService(IApplicationDbContext db, ICurrentUserService currentUser) : IReminderService
{
    private static readonly HashSet<string> AllowedDays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    public async Task<IReadOnlyList<ReminderResponse>> GetAsync(CancellationToken cancellationToken = default) =>
        await db.Reminders.Where(x => x.UserId == currentUser.UserId).OrderBy(x => x.ReminderTime)
            .Select(x => new ReminderResponse(x.Id, x.Title, x.ReminderTime, x.DaysOfWeek, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ReminderResponse> CreateAsync(SaveReminderRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var item = new Reminder { UserId = currentUser.UserId };
        Apply(item, request);
        db.Reminders.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await AchievementService.AwardAsync(db, currentUser.UserId, cancellationToken);
        return ToResponse(item);
    }

    public async Task<ReminderResponse> UpdateAsync(int id, SaveReminderRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var item = await db.Reminders.SingleOrDefaultAsync(x => x.Id == id && x.UserId == currentUser.UserId, cancellationToken)
            ?? throw new AppNotFoundException("Reminder not found.");
        Apply(item, request);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(item);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await db.Reminders.SingleOrDefaultAsync(x => x.Id == id && x.UserId == currentUser.UserId, cancellationToken)
            ?? throw new AppNotFoundException("Reminder not found.");
        db.Reminders.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(SaveReminderRequest request)
    {
        var days = request.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (days.Length == 0 || days.Any(x => !AllowedDays.Contains(x)))
            throw new AppValidationException("Choose at least one valid reminder day.");
    }

    private static void Apply(Reminder item, SaveReminderRequest request)
    {
        item.Title = request.Title.Trim();
        item.ReminderTime = request.ReminderTime;
        item.DaysOfWeek = string.Join(',', request.DaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct());
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
    }

    private static ReminderResponse ToResponse(Reminder item) => new(item.Id, item.Title, item.ReminderTime, item.DaysOfWeek, item.IsActive);
}
