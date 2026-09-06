namespace Domain.Entities;

public sealed class WorkoutExercise : BaseEntity
{
    public int WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public ICollection<ExerciseSet> Sets { get; set; } = [];
}
