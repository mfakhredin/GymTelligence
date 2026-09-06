namespace Domain.Entities;

public sealed class UserBadge
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int BadgeId { get; set; }
    public Badge Badge { get; set; } = null!;
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
