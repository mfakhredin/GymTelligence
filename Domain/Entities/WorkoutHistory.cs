using Domain.Enums;

namespace Domain.Entities;

public sealed class WorkoutHistory : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;
    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public DateOnly CompletedDate { get; set; }
    public int? DurationMinutes { get; set; }
    public FeedbackDifficulty FeedbackDifficulty { get; set; } = FeedbackDifficulty.Normal;
    public string? FeedbackNote { get; set; }
    public int PointsAwarded { get; set; }
}
