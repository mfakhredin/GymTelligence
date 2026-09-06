namespace Domain.Entities;

public sealed class Badge : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointsRequired { get; set; }
    public string Icon { get; set; } = "star";
    public ICollection<UserBadge> UserBadges { get; set; } = [];
}
