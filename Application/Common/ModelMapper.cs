using Application.Models;
using Domain.Entities;

namespace Application.Common;

internal static class ModelMapper
{
    public static UserResponse ToResponse(this User user) => new(
        user.Id, user.Name, user.Email, user.Role.ToString(), user.Age,
        user.Weight, user.Height, user.FitnessLevel.ToString(), user.Goal.ToString(),
        user.SubscriptionStatus.ToString(), user.Points, user.CreatedAt, user.IsActive);

    public static PlanSummary ToSummary(this WorkoutPlan plan) => new(
        plan.Id, plan.Title, plan.Goal.ToString(), plan.Difficulty.ToString(),
        plan.DaysPerWeek, plan.Description, plan.Source.ToString(), plan.Version);

    public static ExerciseResponse ToResponse(this Exercise exercise) => new(
        exercise.Id, exercise.Name, exercise.MuscleGroup, exercise.TargetMuscle,
        exercise.SecondaryMuscles, exercise.Equipment, exercise.Difficulty.ToString(),
        exercise.ShortDescription, exercise.Instructions, exercise.CommonMistakes,
        exercise.SafetyTips, exercise.RecommendedSetsReps, exercise.ScienceNote,
        exercise.IsPublic, exercise.UserId);
}
