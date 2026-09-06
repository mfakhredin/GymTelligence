using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed record StaffOverviewResponse(int Users, int Trainers, int Plans, int Exercises, int CompletedWorkouts, IReadOnlyList<UserResponse> RecentUsers);
public sealed record BackupResponse(string FileName, DateTime CreatedAt, string CreatedBy);
public sealed class UpdateUserAccessRequest
{
    public string Role { get; set; } = "User";
    public string SubscriptionStatus { get; set; } = "Free";
    public bool IsActive { get; set; } = true;
}

public sealed class SaveWorkoutTemplateRequest
{
    [Required, StringLength(160, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    [Required] public string Goal { get; set; } = "GeneralFitness";
    [Required] public string Difficulty { get; set; } = "Beginner";
    [Range(1, 7)] public int DaysPerWeek { get; set; } = 3;
    [Required, StringLength(3000, MinimumLength = 10)] public string Description { get; set; } = string.Empty;
}
