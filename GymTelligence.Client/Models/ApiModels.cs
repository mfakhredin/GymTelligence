using System.ComponentModel.DataAnnotations;

namespace GymTelligence.Client.Models;

public sealed record AuthResponse(string Token, UserModel User);
public sealed record UserModel(int Id, string Name, string Email, string Role, int? Age, decimal? Weight, decimal? Height, string FitnessLevel, string Goal, string SubscriptionStatus, int Points, DateTime CreatedAt, bool IsActive);
public sealed record PlanModel(int Id, string Title, string Goal, string Difficulty, int DaysPerWeek, string Description, string Source, int Version);
public sealed record WorkoutSummaryModel(int Id, int DayNumber, string Title, string? Description, int TotalSets, int CompletedSets);
public sealed record SetModel(int Id, int SetNumber, string Reps, string RestTime, decimal? Weight, bool IsCompleted);
public sealed record WorkoutExerciseModel(int Id, string Name, string MuscleGroup, string Equipment, string? Notes, IReadOnlyList<SetModel> Sets);
public sealed record WorkoutDetailModel(int Id, int DayNumber, string Title, string? Description, IReadOnlyList<WorkoutExerciseModel> Exercises);
public sealed record PlanDetailModel(PlanModel Plan, string? GenerationNotes, IReadOnlyList<WorkoutDetailModel> Workouts);
public sealed record ReminderModel(int Id, string Title, TimeOnly ReminderTime, string DaysOfWeek, bool IsActive);
public sealed record BadgeModel(int Id, string Name, string Description, string Icon, DateTime AwardedAt);
public sealed record HistoryModel(int Id, string WorkoutTitle, DateOnly CompletedDate, int? DurationMinutes, string Difficulty, int PointsAwarded);
public sealed record ProgressPointModel(DateOnly Date, decimal? Weight, decimal? BodyFat, int Workouts);
public sealed record DashboardModel(UserModel User, PlanModel? ActivePlan, WorkoutSummaryModel? TodayWorkout, int WeeklyWorkouts, string Motivation, string Habit, IReadOnlyList<ReminderModel> Reminders, IReadOnlyList<BadgeModel> Badges, IReadOnlyList<HistoryModel> History, IReadOnlyList<ProgressPointModel> Progress);
public sealed record ExerciseModel(int Id, string Name, string MuscleGroup, string TargetMuscle, string? SecondaryMuscles, string Equipment, string Difficulty, string ShortDescription, string Instructions, string CommonMistakes, string SafetyTips, string RecommendedSetsReps, string ScienceNote, bool IsPublic, int? CreatedByUserId);
public sealed record ProgressEntryModel(int Id, DateOnly EntryDate, decimal? Weight, decimal? BodyFatPercentage, int WorkoutsCompleted, string? Notes);
public sealed record ProgressModel(IReadOnlyList<ProgressEntryModel> Entries, IReadOnlyList<ProgressPointModel> Chart, decimal? WeightChange, decimal? BodyFatChange, int WeeklyWorkouts, IReadOnlyList<HistoryModel> History, IReadOnlyList<BadgeModel> Badges);
public sealed record AiMessageModel(int Id, string UserMessage, string AiResponse, DateTime CreatedAt);
public sealed record AskCoachResponse(string Answer, IReadOnlyList<string> Suggestions);
public sealed record StaffOverviewModel(int Users, int Trainers, int Plans, int Exercises, int CompletedWorkouts, IReadOnlyList<UserModel> RecentUsers);
public sealed record SubscriptionModel(string Status, DateTime? CurrentPeriodEnd, bool CheckoutAvailable);
public sealed record CheckoutModel(string Url);
public sealed record ToggleSetModel(bool IsCompleted);
public sealed record ForgotPasswordResponse(string Message, string? DevelopmentResetUrl);
public sealed record BackupModel(string FileName, DateTime CreatedAt, string CreatedBy);

public sealed class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "demo@gymtelligence.test";
    [Required] public string Password { get; set; } = "password";
}

public sealed class RegisterForm
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Compare(nameof(Password))] public string ConfirmPassword { get; set; } = string.Empty;
    [Range(13, 100)] public int Age { get; set; } = 22;
    [Range(20, 400)] public decimal Weight { get; set; } = 72;
    [Range(90, 250)] public decimal Height { get; set; } = 175;
    public string FitnessLevel { get; set; } = "Beginner";
    public string Goal { get; set; } = "GeneralFitness";
}

public sealed class ProfileForm
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Range(13, 100)] public int Age { get; set; }
    [Range(20, 400)] public decimal Weight { get; set; }
    [Range(90, 250)] public decimal Height { get; set; }
    public string FitnessLevel { get; set; } = "Beginner";
    public string Goal { get; set; } = "GeneralFitness";
}

public sealed class PasswordForm
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordForm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordForm
{
    [Required] public string Selector { get; set; } = string.Empty;
    [Required] public string Validator { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
    [Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ProgressForm
{
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Range(20, 400)] public decimal? Weight { get; set; }
    [Range(0, 80)] public decimal? BodyFatPercentage { get; set; }
    [Range(0, 14)] public int WorkoutsCompleted { get; set; }
    [StringLength(1000)] public string? Notes { get; set; }
}

public sealed class ReminderForm
{
    [Required, StringLength(160, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    public TimeOnly ReminderTime { get; set; } = new(18, 0);
    public string DaysOfWeek { get; set; } = "Mon,Wed,Fri";
    public bool IsActive { get; set; } = true;
}

public sealed class ExerciseForm
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string MuscleGroup { get; set; } = "Chest";
    [Required] public string TargetMuscle { get; set; } = string.Empty;
    public string? SecondaryMuscles { get; set; }
    [Required] public string Equipment { get; set; } = "Dumbbell";
    public string Difficulty { get; set; } = "Beginner";
    [Required] public string ShortDescription { get; set; } = string.Empty;
    [Required] public string Instructions { get; set; } = string.Empty;
    [Required] public string CommonMistakes { get; set; } = string.Empty;
    [Required] public string SafetyTips { get; set; } = string.Empty;
    [Required] public string RecommendedSetsReps { get; set; } = "3 × 8–12";
    [Required] public string ScienceNote { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
}

public sealed class WorkoutTemplateForm
{
    [Required, StringLength(160, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = "GeneralFitness";
    public string Difficulty { get; set; } = "Beginner";
    [Range(1, 7)] public int DaysPerWeek { get; set; } = 3;
    [Required, StringLength(3000, MinimumLength = 10)] public string Description { get; set; } = string.Empty;
}

public sealed class UserAccessForm
{
    public string Role { get; set; } = "User";
    public string SubscriptionStatus { get; set; } = "Free";
    public bool IsActive { get; set; } = true;
}
