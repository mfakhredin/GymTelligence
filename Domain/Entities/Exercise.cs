using Domain.Enums;

namespace Domain.Entities;

public sealed class Exercise : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public string TargetMuscle { get; set; } = string.Empty;
    public string? SecondaryMuscles { get; set; }
    public string Equipment { get; set; } = string.Empty;
    public FitnessLevel Difficulty { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string CommonMistakes { get; set; } = string.Empty;
    public string SafetyTips { get; set; } = string.Empty;
    public string RecommendedSetsReps { get; set; } = string.Empty;
    public string ScienceNote { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}
