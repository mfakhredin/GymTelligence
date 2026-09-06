namespace Application.Models;

public sealed record SubscriptionResponse(string Status, DateTime? CurrentPeriodEnd, bool CheckoutAvailable);
public sealed record CheckoutResponse(string Url);
