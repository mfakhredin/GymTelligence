using Application.Models;

namespace Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> MeAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string publicBaseUrl, bool includeDevelopmentLink, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}

public interface IWorkoutService
{
    Task<IReadOnlyList<PlanSummary>> GetPlansAsync(CancellationToken cancellationToken = default);
    Task<PlanDetailResponse> GetPlanAsync(int id, CancellationToken cancellationToken = default);
    Task<PlanDetailResponse> GenerateAsync(CancellationToken cancellationToken = default);
    Task<bool> ToggleSetAsync(int setId, CancellationToken cancellationToken = default);
    Task CompleteWorkoutAsync(int workoutId, WorkoutFeedbackRequest request, CancellationToken cancellationToken = default);
}

public interface IExerciseService
{
    Task<IReadOnlyList<ExerciseResponse>> GetAsync(string? search, string? muscle, string? equipment, string? difficulty, CancellationToken cancellationToken = default);
    Task<ExerciseResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ExerciseResponse> CreateAsync(SaveExerciseRequest request, CancellationToken cancellationToken = default);
    Task<ExerciseResponse> UpdateAsync(int id, SaveExerciseRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IProgressService
{
    Task<ProgressResponse> GetAsync(CancellationToken cancellationToken = default);
    Task<ProgressEntryResponse> AddAsync(AddProgressRequest request, CancellationToken cancellationToken = default);
}

public interface IReminderService
{
    Task<IReadOnlyList<ReminderResponse>> GetAsync(CancellationToken cancellationToken = default);
    Task<ReminderResponse> CreateAsync(SaveReminderRequest request, CancellationToken cancellationToken = default);
    Task<ReminderResponse> UpdateAsync(int id, SaveReminderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAiCoachService
{
    Task<IReadOnlyList<AiMessageResponse>> HistoryAsync(CancellationToken cancellationToken = default);
    Task<AskCoachResponse> AskAsync(AskCoachRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminService
{
    Task<StaffOverviewResponse> OverviewAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserResponse>> UsersAsync(CancellationToken cancellationToken = default);
    Task UpdateUserAsync(int id, UpdateUserAccessRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanSummary>> TemplatesAsync(CancellationToken cancellationToken = default);
    Task<PlanSummary> CreateTemplateAsync(SaveWorkoutTemplateRequest request, CancellationToken cancellationToken = default);
    Task<PlanSummary> UpdateTemplateAsync(int id, SaveWorkoutTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteTemplateAsync(int id, CancellationToken cancellationToken = default);
}

public interface ISubscriptionService
{
    Task<SubscriptionResponse> StatusAsync(CancellationToken cancellationToken = default);
    Task<CheckoutResponse> CreateCheckoutAsync(string successUrl, string cancelUrl, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
