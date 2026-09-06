using Application.Common;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class ExerciseService(IApplicationDbContext db, ICurrentUserService currentUser) : IExerciseService
{
    public async Task<IReadOnlyList<ExerciseResponse>> GetAsync(string? search, string? muscle, string? equipment, string? difficulty, CancellationToken cancellationToken = default)
    {
        var query = db.Exercises.AsNoTracking().Where(x => x.IsPublic || x.UserId == currentUser.UserId);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.ShortDescription.Contains(search));
        if (!string.IsNullOrWhiteSpace(muscle)) query = query.Where(x => x.MuscleGroup == muscle);
        if (!string.IsNullOrWhiteSpace(equipment)) query = query.Where(x => x.Equipment == equipment);
        if (Enum.TryParse<FitnessLevel>(difficulty, true, out var level)) query = query.Where(x => x.Difficulty == level);
        return await query.OrderBy(x => x.MuscleGroup).ThenBy(x => x.Name)
            .Select(x => new ExerciseResponse(x.Id, x.Name, x.MuscleGroup, x.TargetMuscle,
                x.SecondaryMuscles, x.Equipment, x.Difficulty.ToString(), x.ShortDescription,
                x.Instructions, x.CommonMistakes, x.SafetyTips, x.RecommendedSetsReps,
                x.ScienceNote, x.IsPublic, x.UserId)).ToListAsync(cancellationToken);
    }

    public async Task<ExerciseResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await db.Exercises.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == id && (x.IsPublic || x.UserId == currentUser.UserId), cancellationToken)
            ?? throw new AppNotFoundException("Exercise not found.");
        return item.ToResponse();
    }

    public async Task<ExerciseResponse> CreateAsync(SaveExerciseRequest request, CancellationToken cancellationToken = default)
    {
        RequireStaff();
        var item = new Exercise { UserId = currentUser.UserId };
        Apply(item, request);
        db.Exercises.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
    }

    public async Task<ExerciseResponse> UpdateAsync(int id, SaveExerciseRequest request, CancellationToken cancellationToken = default)
    {
        RequireStaff();
        var item = await db.Exercises.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Exercise not found.");
        if (!currentUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && item.UserId != currentUser.UserId)
            throw new AppForbiddenException("You can only edit exercises you created.");
        Apply(item, request);
        await db.SaveChangesAsync(cancellationToken);
        return item.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        RequireStaff();
        var item = await db.Exercises.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new AppNotFoundException("Exercise not found.");
        if (!currentUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) && item.UserId != currentUser.UserId)
            throw new AppForbiddenException("You can only delete exercises you created.");
        db.Exercises.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void RequireStaff()
    {
        if (currentUser.Role is not ("Trainer" or "Admin"))
            throw new AppForbiddenException("Trainer or administrator access is required.");
    }

    private static void Apply(Exercise item, SaveExerciseRequest request)
    {
        if (!Enum.TryParse<FitnessLevel>(request.Difficulty, true, out var difficulty))
            throw new AppValidationException("Choose a valid difficulty.");
        item.Name = request.Name.Trim();
        item.MuscleGroup = request.MuscleGroup.Trim();
        item.TargetMuscle = request.TargetMuscle.Trim();
        item.SecondaryMuscles = request.SecondaryMuscles?.Trim();
        item.Equipment = request.Equipment.Trim();
        item.Difficulty = difficulty;
        item.ShortDescription = request.ShortDescription.Trim();
        item.Instructions = request.Instructions.Trim();
        item.CommonMistakes = request.CommonMistakes.Trim();
        item.SafetyTips = request.SafetyTips.Trim();
        item.RecommendedSetsReps = request.RecommendedSetsReps.Trim();
        item.ScienceNote = request.ScienceNote.Trim();
        item.IsPublic = request.IsPublic;
        item.UpdatedAt = DateTime.UtcNow;
    }
}
