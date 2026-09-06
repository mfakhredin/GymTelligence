using System.Net.Http.Json;
using System.Text.Json;
using Application.Common;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

public sealed class GeminiAiProvider(HttpClient http, IConfiguration configuration) : IAiProvider
{
    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var key = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            return "AI Coach is ready, but a Gemini API key has not been configured yet. You can still use your plans, tracking, reminders, and exercise library.";
        var model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        var response = await http.PostAsJsonAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(key)}",
            new { contents = new[] { new { parts = new[] { new { text = prompt } } } } }, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new AppValidationException("The AI coach is temporarily unavailable. Please try again shortly.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0]
            .GetProperty("text").GetString()?.Trim()
            ?? "I couldn't form a useful response. Try asking in a different way.";
    }
}
