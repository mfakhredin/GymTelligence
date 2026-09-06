using Domain.Enums;

namespace Domain.Entities;

public sealed class Subscription : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Free;
    public DateTime? CurrentPeriodEnd { get; set; }
}
