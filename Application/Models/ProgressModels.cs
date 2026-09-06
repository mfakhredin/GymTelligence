using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed class AddProgressRequest
{
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    [Range(20, 400)] public decimal? Weight { get; set; }
    [Range(0, 80)] public decimal? BodyFatPercentage { get; set; }
    [Range(0, 14)] public int WorkoutsCompleted { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
}

public sealed record ProgressEntryResponse(int Id, DateOnly EntryDate, decimal? Weight, decimal? BodyFatPercentage, int WorkoutsCompleted, string? Notes);
public sealed record ProgressPoint(DateOnly Date, decimal? Weight, decimal? BodyFat, int Workouts);
public sealed record ProgressResponse(IReadOnlyList<ProgressEntryResponse> Entries, IReadOnlyList<ProgressPoint> Chart, decimal? WeightChange, decimal? BodyFatChange, int WeeklyWorkouts, IReadOnlyList<HistoryResponse> History, IReadOnlyList<BadgeResponse> Badges);
