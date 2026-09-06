using Application.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;

public sealed class GymTelligenceContext(DbContextOptions<GymTelligenceContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<ExerciseSet> ExerciseSets => Set<ExerciseSet>();
    public DbSet<UserExerciseSetCompletion> SetCompletions => Set<UserExerciseSetCompletion>();
    public DbSet<WorkoutHistory> WorkoutHistory => Set<WorkoutHistory>();
    public DbSet<ProgressEntry> ProgressEntries => Set<ProgressEntry>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<AiMessage> AiMessages => Set<AiMessage>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<ScientificSource> ScientificSources => Set<ScientificSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureTraining(modelBuilder);
        ConfigureTracking(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(x => x.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        return base.SaveChangesAsync(cancellationToken);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.HasIndex(x => x.Email).IsUnique();
        user.Property(x => x.Name).HasMaxLength(120);
        user.Property(x => x.Email).HasMaxLength(190);
        user.Property(x => x.PasswordHash).HasMaxLength(500);
        user.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
        user.Property(x => x.FitnessLevel).HasConversion<string>().HasMaxLength(30);
        user.Property(x => x.Goal).HasConversion<string>().HasMaxLength(40);
        user.Property(x => x.SubscriptionStatus).HasConversion<string>().HasMaxLength(30);
        user.Property(x => x.Weight).HasPrecision(6, 2);
        user.Property(x => x.Height).HasPrecision(6, 2);
        user.HasMany(x => x.WorkoutPlans).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => x.Selector).IsUnique();
    }

    private static void ConfigureTraining(ModelBuilder modelBuilder)
    {
        var plan = modelBuilder.Entity<WorkoutPlan>();
        plan.Property(x => x.Title).HasMaxLength(160);
        plan.Property(x => x.Goal).HasConversion<string>().HasMaxLength(40);
        plan.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(30);
        plan.Property(x => x.Source).HasConversion<string>().HasMaxLength(30);
        plan.HasMany(x => x.Workouts).WithOne(x => x.WorkoutPlan).HasForeignKey(x => x.WorkoutPlanId).OnDelete(DeleteBehavior.Cascade);

        var exercise = modelBuilder.Entity<Exercise>();
        exercise.Property(x => x.Name).HasMaxLength(160);
        exercise.Property(x => x.MuscleGroup).HasMaxLength(80);
        exercise.Property(x => x.TargetMuscle).HasMaxLength(120);
        exercise.Property(x => x.Equipment).HasMaxLength(80);
        exercise.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(30);
        exercise.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);

        var workoutExercise = modelBuilder.Entity<WorkoutExercise>();
        workoutExercise.HasOne(x => x.Workout).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.Cascade);
        workoutExercise.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        workoutExercise.HasIndex(x => new { x.WorkoutId, x.SortOrder }).IsUnique();
        modelBuilder.Entity<ExerciseSet>().Property(x => x.Weight).HasPrecision(7, 2);
        modelBuilder.Entity<UserExerciseSetCompletion>().HasIndex(x => new { x.UserId, x.ExerciseSetId }).IsUnique();
        modelBuilder.Entity<UserExerciseSetCompletion>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserExerciseSetCompletion>().HasOne(x => x.ExerciseSet).WithMany(x => x.Completions).HasForeignKey(x => x.ExerciseSetId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTracking(ModelBuilder modelBuilder)
    {
        var history = modelBuilder.Entity<WorkoutHistory>();
        history.Property(x => x.FeedbackDifficulty).HasConversion<string>().HasMaxLength(20);
        history.HasIndex(x => new { x.UserId, x.WorkoutId, x.CompletedDate }).IsUnique();
        history.HasOne(x => x.User).WithMany(x => x.WorkoutHistory).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        history.HasOne(x => x.WorkoutPlan).WithMany().HasForeignKey(x => x.WorkoutPlanId).OnDelete(DeleteBehavior.NoAction);
        history.HasOne(x => x.Workout).WithMany().HasForeignKey(x => x.WorkoutId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ProgressEntry>().Property(x => x.Weight).HasPrecision(6, 2);
        modelBuilder.Entity<ProgressEntry>().Property(x => x.BodyFatPercentage).HasPrecision(5, 2);
        modelBuilder.Entity<ProgressEntry>().HasIndex(x => new { x.UserId, x.EntryDate });
        modelBuilder.Entity<Reminder>().Property(x => x.Title).HasMaxLength(160);
        modelBuilder.Entity<Subscription>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<Subscription>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Badge>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<UserBadge>().HasKey(x => new { x.UserId, x.BadgeId });
        modelBuilder.Entity<UserBadge>().HasOne(x => x.User).WithMany(x => x.UserBadges).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserBadge>().HasOne(x => x.Badge).WithMany(x => x.UserBadges).HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
    }
}
