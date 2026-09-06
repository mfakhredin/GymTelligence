namespace Domain.Entities;

public sealed class AiMessage : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string UserMessage { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
}
