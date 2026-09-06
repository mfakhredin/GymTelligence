using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed class AskCoachRequest
{
    [Required, StringLength(1000, MinimumLength = 2)] public string Message { get; set; } = string.Empty;
}

public sealed record AiMessageResponse(int Id, string UserMessage, string AiResponse, DateTime CreatedAt);
public sealed record AskCoachResponse(string Answer, IReadOnlyList<string> Suggestions);
