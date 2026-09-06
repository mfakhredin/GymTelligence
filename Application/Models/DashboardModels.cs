namespace Application.Models;

public sealed record DashboardResponse(
    UserResponse User,
    PlanSummary? ActivePlan,
    WorkoutSummary? TodayWorkout,
    int WeeklyWorkouts,
    string Motivation,
    string Habit,
    IReadOnlyList<ReminderResponse> Reminders,
    IReadOnlyList<BadgeResponse> Badges,
    IReadOnlyList<HistoryResponse> History,
    IReadOnlyList<ProgressPoint> Progress);

public sealed record BadgeResponse(int Id, string Name, string Description, string Icon, DateTime AwardedAt);
public sealed record HistoryResponse(int Id, string WorkoutTitle, DateOnly CompletedDate, int? DurationMinutes, string Difficulty, int PointsAwarded);
