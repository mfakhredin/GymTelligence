namespace Domain.Entities;

public sealed class Workout : BaseEntity
{
    public int WorkoutPlanId { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;
    public int DayNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<WorkoutExercise> Exercises { get; set; } = [];
}
