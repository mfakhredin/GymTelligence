using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common;
using Application.Models;
using Infrastructure.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class JsonBackupService(
    GymTelligenceContext db,
    ICurrentUserService currentUser,
    IWebHostEnvironment environment) : IBackupService
{
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public async Task<BackupResponse> CreateAsync(CancellationToken cancellationToken = default)
    {
        var admin = await db.Users.AsNoTracking().SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
        var createdAt = DateTime.UtcNow;
        var backup = new BackupEnvelope(
            new BackupMetadata(createdAt, admin.Email, "GymTelligence structured JSON backup v1"),
            new Dictionary<string, JsonElement>
            {
                ["Users"] = await ExportAsync<Domain.Entities.User>(cancellationToken),
                ["PasswordResetTokens"] = await ExportAsync<Domain.Entities.PasswordResetToken>(cancellationToken),
                ["WorkoutPlans"] = await ExportAsync<Domain.Entities.WorkoutPlan>(cancellationToken),
                ["Workouts"] = await ExportAsync<Domain.Entities.Workout>(cancellationToken),
                ["Exercises"] = await ExportAsync<Domain.Entities.Exercise>(cancellationToken),
                ["WorkoutExercises"] = await ExportAsync<Domain.Entities.WorkoutExercise>(cancellationToken),
                ["ExerciseSets"] = await ExportAsync<Domain.Entities.ExerciseSet>(cancellationToken),
                ["SetCompletions"] = await ExportAsync<Domain.Entities.UserExerciseSetCompletion>(cancellationToken),
                ["WorkoutHistory"] = await ExportAsync<Domain.Entities.WorkoutHistory>(cancellationToken),
                ["ProgressEntries"] = await ExportAsync<Domain.Entities.ProgressEntry>(cancellationToken),
                ["Reminders"] = await ExportAsync<Domain.Entities.Reminder>(cancellationToken),
                ["AiMessages"] = await ExportAsync<Domain.Entities.AiMessage>(cancellationToken),
                ["Subscriptions"] = await ExportAsync<Domain.Entities.Subscription>(cancellationToken),
                ["Badges"] = await ExportAsync<Domain.Entities.Badge>(cancellationToken),
                ["UserBadges"] = await ExportAsync<Domain.Entities.UserBadge>(cancellationToken),
                ["ScientificSources"] = await ExportAsync<Domain.Entities.ScientificSource>(cancellationToken)
            });

        var directory = BackupDirectory();
        Directory.CreateDirectory(directory);
        var fileName = $"gymtelligence_backup_{createdAt:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
        await File.WriteAllTextAsync(Path.Combine(directory, fileName), JsonSerializer.Serialize(backup, options), cancellationToken);
        return new BackupResponse(fileName, createdAt, admin.Email);
    }

    public async Task<IReadOnlyList<BackupResponse>> RecentAsync(CancellationToken cancellationToken = default)
    {
        var directory = BackupDirectory();
        if (!Directory.Exists(directory)) return [];
        var result = new List<BackupResponse>();
        foreach (var file in new DirectoryInfo(directory).GetFiles("gymtelligence_backup_*.json")
                     .OrderByDescending(x => x.CreationTimeUtc).Take(10))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = file.OpenRead();
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var metadata = json.RootElement.GetProperty("metadata");
                result.Add(new BackupResponse(file.Name,
                    metadata.GetProperty("createdAt").GetDateTime(),
                    metadata.GetProperty("createdBy").GetString() ?? "Administrator"));
            }
            catch (JsonException)
            {
                result.Add(new BackupResponse(file.Name, file.CreationTimeUtc, "Unknown"));
            }
        }
        return result;
    }

    private async Task<JsonElement> ExportAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class =>
        JsonSerializer.SerializeToElement(await db.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken), options);

    private string BackupDirectory() => Path.Combine(environment.ContentRootPath, "App_Data", "backups");

    private sealed record BackupEnvelope(BackupMetadata Metadata, Dictionary<string, JsonElement> Data);
    private sealed record BackupMetadata(DateTime CreatedAt, string CreatedBy, string Format);
}
