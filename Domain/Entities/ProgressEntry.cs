namespace Domain.Entities;

public sealed class ProgressEntry : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly EntryDate { get; set; }
    public decimal? Weight { get; set; }
    public decimal? BodyFatPercentage { get; set; }
    public int WorkoutsCompleted { get; set; }
    public string? Notes { get; set; }
}
