using Domain.Enums;

namespace Domain.Entities;

public sealed class User : BaseEntity
{
    public UserRole Role { get; set; } = UserRole.User;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int? Age { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public FitnessLevel FitnessLevel { get; set; } = FitnessLevel.Beginner;
    public FitnessGoal Goal { get; set; } = FitnessGoal.GeneralFitness;
    public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Free;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public int Points { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = [];
    public ICollection<ProgressEntry> ProgressEntries { get; set; } = [];
    public ICollection<Reminder> Reminders { get; set; } = [];
    public ICollection<WorkoutHistory> WorkoutHistory { get; set; } = [];
    public ICollection<UserBadge> UserBadges { get; set; } = [];
}
