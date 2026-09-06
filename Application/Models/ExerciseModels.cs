using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed record ExerciseResponse(
    int Id, string Name, string MuscleGroup, string TargetMuscle,
    string? SecondaryMuscles, string Equipment, string Difficulty,
    string ShortDescription, string Instructions, string CommonMistakes,
    string SafetyTips, string RecommendedSetsReps, string ScienceNote,
    bool IsPublic, int? CreatedByUserId);

public sealed class SaveExerciseRequest
{
    [Required, StringLength(160, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(80)] public string MuscleGroup { get; set; } = string.Empty;
    [Required, StringLength(120)] public string TargetMuscle { get; set; } = string.Empty;
    [StringLength(255)] public string? SecondaryMuscles { get; set; }
    [Required, StringLength(80)] public string Equipment { get; set; } = string.Empty;
    [Required] public string Difficulty { get; set; } = "Beginner";
    [Required, StringLength(255)] public string ShortDescription { get; set; } = string.Empty;
    [Required, StringLength(4000)] public string Instructions { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string CommonMistakes { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string SafetyTips { get; set; } = string.Empty;
    [Required, StringLength(120)] public string RecommendedSetsReps { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string ScienceNote { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}
