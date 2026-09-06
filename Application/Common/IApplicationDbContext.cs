using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<WorkoutPlan> WorkoutPlans { get; }
    DbSet<Workout> Workouts { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<WorkoutExercise> WorkoutExercises { get; }
    DbSet<ExerciseSet> ExerciseSets { get; }
    DbSet<UserExerciseSetCompletion> SetCompletions { get; }
    DbSet<WorkoutHistory> WorkoutHistory { get; }
    DbSet<ProgressEntry> ProgressEntries { get; }
    DbSet<Reminder> Reminders { get; }
    DbSet<AiMessage> AiMessages { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Badge> Badges { get; }
    DbSet<UserBadge> UserBadges { get; }
    DbSet<ScientificSource> ScientificSources { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
