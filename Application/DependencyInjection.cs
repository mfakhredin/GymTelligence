using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IWorkoutService, WorkoutService>();
        services.AddScoped<IExerciseService, ExerciseService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IAiCoachService, AiCoachService>();
        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}
