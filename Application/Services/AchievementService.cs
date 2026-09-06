using Application.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

internal static class AchievementService
{
    public static async Task AwardAsync(IApplicationDbContext db, int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleAsync(x => x.Id == userId, cancellationToken);
        var workoutCount = await db.WorkoutHistory.CountAsync(x => x.UserId == userId, cancellationToken);
        var hasReminder = await db.Reminders.AnyAsync(x => x.UserId == userId, cancellationToken);
        var earned = await db.UserBadges.Where(x => x.UserId == userId).Select(x => x.BadgeId).ToListAsync(cancellationToken);
        var badges = await db.Badges.Where(x => !earned.Contains(x.Id) &&
            (x.PointsRequired <= user.Points ||
             (x.Slug == "first-workout" && workoutCount >= 1) ||
             (x.Slug == "consistency-builder" && workoutCount >= 8) ||
             (x.Slug == "habit-builder" && hasReminder))).ToListAsync(cancellationToken);
        foreach (var badge in badges)
            db.UserBadges.Add(new UserBadge { UserId = userId, BadgeId = badge.Id });
        if (badges.Count > 0) await db.SaveChangesAsync(cancellationToken);
    }
}
