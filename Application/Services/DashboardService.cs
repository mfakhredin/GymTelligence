using Application.Common;
using Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class DashboardService(IApplicationDbContext db, ICurrentUserService currentUser) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
        var plan = await db.WorkoutPlans
            .Where(x => x.UserId == user.Id || x.IsTemplate)
            .OrderByDescending(x => x.UserId == user.Id).ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var workout = plan is null ? null : await db.Workouts
            .Where(x => x.WorkoutPlanId == plan.Id).OrderBy(x => x.DayNumber)
            .Select(x => new WorkoutSummary(
                x.Id, x.DayNumber, x.Title, x.Description,
                x.Exercises.SelectMany(e => e.Sets).Count(),
                x.Exercises.SelectMany(e => e.Sets)
                    .Count(s => s.Completions.Any(c => c.UserId == user.Id))))
            .FirstOrDefaultAsync(cancellationToken);
        var weekStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-6));
        var weekly = await db.WorkoutHistory.CountAsync(
            x => x.UserId == user.Id && x.CompletedDate >= weekStart, cancellationToken);
        var reminders = await db.Reminders.Where(x => x.UserId == user.Id && x.IsActive)
            .OrderBy(x => x.ReminderTime).Take(4)
            .Select(x => new ReminderResponse(x.Id, x.Title, x.ReminderTime, x.DaysOfWeek, x.IsActive))
            .ToListAsync(cancellationToken);
        var badges = await db.UserBadges.Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.AwardedAt)
            .Select(x => new BadgeResponse(x.BadgeId, x.Badge.Name, x.Badge.Description, x.Badge.Icon, x.AwardedAt))
            .ToListAsync(cancellationToken);
        var history = await db.WorkoutHistory.Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CompletedDate).Take(5)
            .Select(x => new HistoryResponse(x.Id, x.Workout.Title, x.CompletedDate,
                x.DurationMinutes, x.FeedbackDifficulty.ToString(), x.PointsAwarded))
            .ToListAsync(cancellationToken);
        var progress = await db.ProgressEntries.Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.EntryDate).Take(8).OrderBy(x => x.EntryDate)
            .Select(x => new ProgressPoint(x.EntryDate, x.Weight, x.BodyFatPercentage, x.WorkoutsCompleted))
            .ToListAsync(cancellationToken);

        return new DashboardResponse(user.ToResponse(), plan?.ToSummary(), workout, weekly,
            Motivation(weekly), Habit(user.Goal.ToString(), weekly), reminders, badges, history, progress);
    }

    private static string Motivation(int count) => count switch
    {
        0 => "Start with one focused session today. Consistency beats perfection.",
        < 3 => "Good start. One more session turns intention into rhythm.",
        < 5 => "Strong week. Keep the form controlled and recovery honest.",
        _ => "Excellent consistency. Protect your sleep so progress keeps compounding."
    };

    private static string Habit(string goal, int count) => goal switch
    {
        "LoseFat" => "Add a ten-minute walk after one meal and keep protein consistent.",
        "BuildMuscle" => "Plan a protein-rich meal after training and protect 7–9 hours of sleep.",
        _ when count == 0 => "Set one reminder and complete a short warm-up today.",
        _ => "Warm up gradually, record your sets, and earn every load increase."
    };
}
