using Application.Common;
using Application.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class AiCoachService(IApplicationDbContext db, ICurrentUserService currentUser, IAiProvider provider) : IAiCoachService
{
    private static readonly string[] DangerousTerms = ["steroid", "trenbolone", "anavar", "dianabol", "sarms", "clenbuterol", "purge", "starve"];
    private static readonly string[] InjuryTerms = ["chest pain", "cannot breathe", "passed out", "severe injury", "broken bone", "diagnose"];

    public async Task<IReadOnlyList<AiMessageResponse>> HistoryAsync(CancellationToken cancellationToken = default) =>
        await db.AiMessages.Where(x => x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.CreatedAt).Take(30).OrderBy(x => x.CreatedAt)
            .Select(x => new AiMessageResponse(x.Id, x.UserMessage, x.AiResponse, x.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<AskCoachResponse> AskAsync(AskCoachRequest request, CancellationToken cancellationToken = default)
    {
        var message = request.Message.Trim();
        var lower = message.ToLowerInvariant();
        string answer;
        if (InjuryTerms.Any(lower.Contains))
            answer = "I can’t diagnose an injury or urgent symptom. Stop training and contact a qualified medical professional; call local emergency services for severe or sudden symptoms.";
        else if (DangerousTerms.Any(lower.Contains))
            answer = "I can’t help with performance-enhancing drugs, starvation, or dangerous practices. I can help you build a safe training, nutrition, and recovery plan instead.";
        else
        {
            var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
            var prompt = $"You are GymTelligence, a concise evidence-aware fitness coach. User profile: level={user.FitnessLevel}, goal={user.Goal}, age={user.Age}, weight={user.Weight}kg. Give practical, safe guidance. Do not diagnose illness, prescribe medication, or recommend PEDs. Encourage professional care for pain or medical risk. Question: {message}";
            answer = await provider.AskAsync(prompt, cancellationToken);
        }

        db.AiMessages.Add(new AiMessage { UserId = currentUser.UserId, UserMessage = message, AiResponse = answer });
        await db.SaveChangesAsync(cancellationToken);
        return new AskCoachResponse(answer,
            ["Adapt my next workout", "How should I recover today?", "Explain progressive overload"]);
    }
}
