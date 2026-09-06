namespace Domain.Entities;

public sealed class UserExerciseSetCompletion : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ExerciseSetId { get; set; }
    public ExerciseSet ExerciseSet { get; set; } = null!;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
