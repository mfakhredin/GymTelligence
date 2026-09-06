namespace Domain.Entities;

public sealed class ExerciseSet : BaseEntity
{
    public int WorkoutExerciseId { get; set; }
    public WorkoutExercise WorkoutExercise { get; set; } = null!;
    public int SetNumber { get; set; }
    public string Reps { get; set; } = string.Empty;
    public string RestTime { get; set; } = string.Empty;
    public decimal? Weight { get; set; }
    public ICollection<UserExerciseSetCompletion> Completions { get; set; } = [];
}
