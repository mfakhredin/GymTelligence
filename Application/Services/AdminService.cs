using Application.Common;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class AdminService(IApplicationDbContext db, ICurrentUserService currentUser) : IAdminService
{
    public async Task<StaffOverviewResponse> OverviewAsync(CancellationToken cancellationToken = default)
    {
        RequireStaff();
        var recent = await db.Users.OrderByDescending(x => x.CreatedAt).Take(6)
            .Select(x => new UserResponse(x.Id, x.Name, x.Email, x.Role.ToString(), x.Age, x.Weight,
                x.Height, x.FitnessLevel.ToString(), x.Goal.ToString(), x.SubscriptionStatus.ToString(),
                x.Points, x.CreatedAt, x.IsActive)).ToListAsync(cancellationToken);
        return new StaffOverviewResponse(
            await db.Users.CountAsync(x => x.Role == UserRole.User, cancellationToken),
            await db.Users.CountAsync(x => x.Role == UserRole.Trainer, cancellationToken),
            await db.WorkoutPlans.CountAsync(cancellationToken),
            await db.Exercises.CountAsync(cancellationToken),
            await db.WorkoutHistory.CountAsync(cancellationToken), recent);
    }

    public async Task<IReadOnlyList<UserResponse>> UsersAsync(CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        return await db.Users.OrderByDescending(x => x.CreatedAt)
            .Select(x => new UserResponse(x.Id, x.Name, x.Email, x.Role.ToString(), x.Age, x.Weight,
                x.Height, x.FitnessLevel.ToString(), x.Goal.ToString(), x.SubscriptionStatus.ToString(),
                x.Points, x.CreatedAt, x.IsActive)).ToListAsync(cancellationToken);
    }

    public async Task UpdateUserAsync(int id, UpdateUserAccessRequest request, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        if (id == currentUser.UserId)
            throw new AppValidationException("You cannot change your own role or access status.");
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new AppValidationException("Choose a valid role.");
        if (!Enum.TryParse<SubscriptionStatus>(request.SubscriptionStatus, true, out var status))
            throw new AppValidationException("Choose a valid subscription status.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("User not found.");
        user.Role = role;
        user.SubscriptionStatus = status;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PlanSummary>> TemplatesAsync(CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        return await db.WorkoutPlans.AsNoTracking().Where(x => x.IsTemplate)
            .OrderBy(x => x.Goal).ThenBy(x => x.Difficulty).ThenBy(x => x.Title)
            .Select(x => new PlanSummary(x.Id, x.Title, x.Goal.ToString(), x.Difficulty.ToString(),
                x.DaysPerWeek, x.Description, x.Source.ToString(), x.Version))
            .ToListAsync(cancellationToken);
    }

    public async Task<PlanSummary> CreateTemplateAsync(SaveWorkoutTemplateRequest request, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var (goal, difficulty) = ParseTemplateEnums(request);
        var plan = new WorkoutPlan
        {
            Title = request.Title.Trim(), Goal = goal, Difficulty = difficulty,
            DaysPerWeek = request.DaysPerWeek, Description = request.Description.Trim(),
            IsTemplate = true, Source = PlanSource.Trainer,
            GenerationNotes = "Created by an administrator."
        };
        db.WorkoutPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        return plan.ToSummary();
    }

    public async Task<PlanSummary> UpdateTemplateAsync(int id, SaveWorkoutTemplateRequest request, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var (goal, difficulty) = ParseTemplateEnums(request);
        var plan = await db.WorkoutPlans.SingleOrDefaultAsync(x => x.Id == id && x.IsTemplate, cancellationToken)
            ?? throw new AppNotFoundException("Workout template not found.");
        plan.Title = request.Title.Trim();
        plan.Goal = goal;
        plan.Difficulty = difficulty;
        plan.DaysPerWeek = request.DaysPerWeek;
        plan.Description = request.Description.Trim();
        plan.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return plan.ToSummary();
    }

    public async Task DeleteTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var plan = await db.WorkoutPlans.SingleOrDefaultAsync(x => x.Id == id && x.IsTemplate, cancellationToken)
            ?? throw new AppNotFoundException("Workout template not found.");
        var history = await db.WorkoutHistory.Where(x => x.WorkoutPlanId == id).ToListAsync(cancellationToken);
        db.WorkoutHistory.RemoveRange(history);
        db.WorkoutPlans.Remove(plan);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static (FitnessGoal Goal, FitnessLevel Difficulty) ParseTemplateEnums(SaveWorkoutTemplateRequest request)
    {
        if (!Enum.TryParse<FitnessGoal>(request.Goal.Replace(" ", string.Empty), true, out var goal))
            throw new AppValidationException("Choose a valid goal.");
        if (!Enum.TryParse<FitnessLevel>(request.Difficulty, true, out var difficulty))
            throw new AppValidationException("Choose a valid difficulty.");
        return (goal, difficulty);
    }

    private void RequireStaff()
    {
        if (currentUser.Role is not ("Trainer" or "Admin"))
            throw new AppForbiddenException("Trainer or administrator access is required.");
    }

    private void RequireAdmin()
    {
        if (currentUser.Role != "Admin")
            throw new AppForbiddenException("Administrator access is required.");
    }
}
