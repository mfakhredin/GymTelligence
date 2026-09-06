using Application.Common;
using Application.Services;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContext<GymTelligenceContext>(options => options.UseSqlServer(
            connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<GymTelligenceContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IEmailService, SmtpEmailService>();
        services.AddScoped<IBackupService, JsonBackupService>();
        services.AddHttpClient<IAiProvider, GeminiAiProvider>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient();
        services.AddScoped<ISubscriptionService, StripeSubscriptionService>();
        return services;
    }
}
