using Domain.Enums;

namespace Domain.Entities;

public sealed class WorkoutPlan : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public FitnessGoal Goal { get; set; }
    public FitnessLevel Difficulty { get; set; }
    public int DaysPerWeek { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsTemplate { get; set; }
    public PlanSource Source { get; set; } = PlanSource.Seed;
    public string? GenerationNotes { get; set; }
    public int Version { get; set; } = 1;
    public ICollection<Workout> Workouts { get; set; } = [];
}
