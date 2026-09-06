using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed record PlanSummary(int Id, string Title, string Goal, string Difficulty, int DaysPerWeek, string Description, string Source, int Version);
public sealed record WorkoutSummary(int Id, int DayNumber, string Title, string? Description, int TotalSets, int CompletedSets);
public sealed record ExerciseSetResponse(int Id, int SetNumber, string Reps, string RestTime, decimal? Weight, bool IsCompleted);
public sealed record WorkoutExerciseResponse(int Id, string Name, string MuscleGroup, string Equipment, string? Notes, IReadOnlyList<ExerciseSetResponse> Sets);
public sealed record WorkoutDetailResponse(int Id, int DayNumber, string Title, string? Description, IReadOnlyList<WorkoutExerciseResponse> Exercises);
public sealed record PlanDetailResponse(PlanSummary Plan, string? GenerationNotes, IReadOnlyList<WorkoutDetailResponse> Workouts);

public sealed class WorkoutFeedbackRequest
{
    [Range(0, 600)] public int DurationMinutes { get; set; }
    [Required] public string Difficulty { get; set; } = "Normal";
    [StringLength(1000)] public string? Note { get; set; }
}
