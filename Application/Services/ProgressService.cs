using Application.Common;
using Application.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class ProgressService(IApplicationDbContext db, ICurrentUserService currentUser) : IProgressService
{
    public async Task<ProgressResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var entries = await db.ProgressEntries.Where(x => x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.EntryDate).Take(20)
            .Select(x => new ProgressEntryResponse(x.Id, x.EntryDate, x.Weight, x.BodyFatPercentage, x.WorkoutsCompleted, x.Notes))
            .ToListAsync(cancellationToken);
        var chart = entries.OrderBy(x => x.EntryDate)
            .Select(x => new ProgressPoint(x.EntryDate, x.Weight, x.BodyFatPercentage, x.WorkoutsCompleted)).ToList();
        var oldest = chart.FirstOrDefault();
        var newest = chart.LastOrDefault();
        var history = await db.WorkoutHistory.Where(x => x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.CompletedDate).Take(10)
            .Select(x => new HistoryResponse(x.Id, x.Workout.Title, x.CompletedDate, x.DurationMinutes,
                x.FeedbackDifficulty.ToString(), x.PointsAwarded)).ToListAsync(cancellationToken);
        var badges = await db.UserBadges.Where(x => x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.AwardedAt)
            .Select(x => new BadgeResponse(x.BadgeId, x.Badge.Name, x.Badge.Description, x.Badge.Icon, x.AwardedAt))
            .ToListAsync(cancellationToken);
        var weekStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-6));
        var weekly = await db.WorkoutHistory.CountAsync(x => x.UserId == currentUser.UserId && x.CompletedDate >= weekStart, cancellationToken);
        return new ProgressResponse(entries, chart,
            oldest?.Weight is not null && newest?.Weight is not null ? newest.Weight - oldest.Weight : null,
            oldest?.BodyFat is not null && newest?.BodyFat is not null ? newest.BodyFat - oldest.BodyFat : null,
            weekly, history, badges);
    }

    public async Task<ProgressEntryResponse> AddAsync(AddProgressRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EntryDate > DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new AppValidationException("Progress date cannot be in the future.");
        var entry = new ProgressEntry
        {
            UserId = currentUser.UserId, EntryDate = request.EntryDate,
            Weight = request.Weight, BodyFatPercentage = request.BodyFatPercentage,
            WorkoutsCompleted = request.WorkoutsCompleted, Notes = request.Notes?.Trim()
        };
        db.ProgressEntries.Add(entry);
        var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
        user.Points += 5 + request.WorkoutsCompleted * 20;
        if (request.Weight.HasValue) user.Weight = request.Weight;
        await db.SaveChangesAsync(cancellationToken);
        await AchievementService.AwardAsync(db, currentUser.UserId, cancellationToken);
        return new ProgressEntryResponse(entry.Id, entry.EntryDate, entry.Weight, entry.BodyFatPercentage, entry.WorkoutsCompleted, entry.Notes);
    }
}
