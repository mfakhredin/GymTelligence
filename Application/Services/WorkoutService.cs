using Application.Common;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class WorkoutService(IApplicationDbContext db, ICurrentUserService currentUser) : IWorkoutService
{
    public async Task<IReadOnlyList<PlanSummary>> GetPlansAsync(CancellationToken cancellationToken = default) =>
        await db.WorkoutPlans.AsNoTracking()
            .Where(x => x.IsTemplate || x.UserId == currentUser.UserId)
            .OrderByDescending(x => x.UserId == currentUser.UserId).ThenByDescending(x => x.CreatedAt)
            .Select(x => new PlanSummary(x.Id, x.Title, x.Goal.ToString(), x.Difficulty.ToString(),
                x.DaysPerWeek, x.Description, x.Source.ToString(), x.Version))
            .ToListAsync(cancellationToken);

    public async Task<PlanDetailResponse> GetPlanAsync(int id, CancellationToken cancellationToken = default)
    {
        var plan = await db.WorkoutPlans.AsNoTracking()
            .Include(x => x.Workouts.OrderBy(w => w.DayNumber))
                .ThenInclude(x => x.Exercises.OrderBy(e => e.SortOrder))
                    .ThenInclude(x => x.Exercise)
            .Include(x => x.Workouts).ThenInclude(x => x.Exercises).ThenInclude(x => x.Sets.OrderBy(s => s.SetNumber))
                .ThenInclude(x => x.Completions.Where(c => c.UserId == currentUser.UserId))
            .SingleOrDefaultAsync(x => x.Id == id && (x.IsTemplate || x.UserId == currentUser.UserId), cancellationToken)
            ?? throw new AppNotFoundException("Workout plan not found.");

        var workouts = plan.Workouts.OrderBy(x => x.DayNumber).Select(workout =>
            new WorkoutDetailResponse(workout.Id, workout.DayNumber, workout.Title, workout.Description,
                workout.Exercises.OrderBy(x => x.SortOrder).Select(item =>
                    new WorkoutExerciseResponse(item.Id, item.Exercise.Name, item.Exercise.MuscleGroup,
                        item.Exercise.Equipment, item.Notes,
                        item.Sets.OrderBy(x => x.SetNumber).Select(set =>
                            new ExerciseSetResponse(set.Id, set.SetNumber, set.Reps, set.RestTime,
                                set.Weight, set.Completions.Count != 0)).ToList())).ToList())).ToList();
        return new PlanDetailResponse(plan.ToSummary(), plan.GenerationNotes, workouts);
    }

    public async Task<PlanDetailResponse> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
        var feedback = await db.WorkoutHistory.Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CompletedDate).Take(5).Select(x => x.FeedbackDifficulty).ToListAsync(cancellationToken);
        var recentProgress = await db.ProgressEntries.Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.EntryDate).Take(2).ToListAsync(cancellationToken);
        var hardCount = feedback.Count(x => x == FeedbackDifficulty.Hard);
        var easyCount = feedback.Count(x => x == FeedbackDifficulty.Easy);
        var days = user.FitnessLevel switch { FitnessLevel.Beginner => 3, FitnessLevel.Intermediate => 4, _ => 5 };
        if (hardCount >= 2) days = Math.Max(2, days - 1);
        if (easyCount >= 2) days = Math.Min(6, days + 1);
        var sets = user.FitnessLevel == FitnessLevel.Beginner ? 3 : 4;
        if (hardCount >= 2) sets = Math.Max(2, sets - 1);
        var version = (await db.WorkoutPlans.Where(x => x.UserId == user.Id)
            .MaxAsync(x => (int?)x.Version, cancellationToken) ?? 0) + 1;

        var notes = new List<string>
        {
            $"Profile: {user.FitnessLevel} level, {GoalLabel(user.Goal)} goal, {days} training days.",
            "Exercise selection is matched to your level and organized around recoverable weekly volume."
        };
        if (hardCount >= 2) notes.Add("Recovery adjustment: recent hard feedback reduced weekly volume.");
        if (easyCount >= 2) notes.Add("Progression adjustment: recent easy feedback increased weekly volume.");
        if (recentProgress.Count == 2 && user.Goal == FitnessGoal.LoseFat &&
            recentProgress[0].Weight >= recentProgress[1].Weight)
            notes.Add("Fat-loss adjustment: conditioning density is emphasized because weight has not decreased.");

        var plan = new WorkoutPlan
        {
            UserId = user.Id,
            Title = $"{GoalLabel(user.Goal)} / Adaptive Block {version:D2}",
            Goal = user.Goal,
            Difficulty = user.FitnessLevel,
            DaysPerWeek = days,
            Description = "A living training block generated from your profile, recent performance, feedback, and recovery signals.",
            Source = PlanSource.Generated,
            GenerationNotes = string.Join('\n', notes),
            Version = version
        };
        db.WorkoutPlans.Add(plan);

        var templates = Templates(user.Goal).Take(days).ToList();
        var exercises = await db.Exercises.Where(x => x.IsPublic && x.Difficulty <= user.FitnessLevel)
            .OrderBy(x => x.Id).ToListAsync(cancellationToken);
        foreach (var (template, dayIndex) in templates.Select((value, index) => (value, index)))
        {
            var workout = new Workout { WorkoutPlan = plan, DayNumber = dayIndex + 1, Title = template.Title, Description = template.Description };
            plan.Workouts.Add(workout);
            foreach (var (muscle, sortIndex) in template.Muscles.Select((value, index) => (value, index)))
            {
                var candidates = exercises.Where(x => x.MuscleGroup.Equals(muscle, StringComparison.OrdinalIgnoreCase)).ToList();
                if (candidates.Count == 0) continue;
                var exercise = candidates[(version + sortIndex) % candidates.Count];
                var item = new WorkoutExercise { Workout = workout, Exercise = exercise, SortOrder = sortIndex + 1, Notes = template.Note };
                workout.Exercises.Add(item);
                for (var setNumber = 1; setNumber <= sets; setNumber++)
                    item.Sets.Add(new ExerciseSet
                    {
                        WorkoutExercise = item, SetNumber = setNumber,
                        Reps = RepScheme(user.Goal, exercise.Equipment),
                        RestTime = RestScheme(user.Goal, exercise.Equipment)
                    });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return await GetPlanAsync(plan.Id, cancellationToken);
    }

    public async Task<bool> ToggleSetAsync(int setId, CancellationToken cancellationToken = default)
    {
        var set = await db.ExerciseSets.Include(x => x.WorkoutExercise).ThenInclude(x => x.Workout)
            .ThenInclude(x => x.WorkoutPlan).SingleOrDefaultAsync(x => x.Id == setId, cancellationToken)
            ?? throw new AppNotFoundException("Set not found.");
        if (!set.WorkoutExercise.Workout.WorkoutPlan.IsTemplate && set.WorkoutExercise.Workout.WorkoutPlan.UserId != currentUser.UserId)
            throw new AppForbiddenException("You do not have access to this workout set.");
        var completion = await db.SetCompletions.SingleOrDefaultAsync(
            x => x.ExerciseSetId == setId && x.UserId == currentUser.UserId, cancellationToken);
        if (completion is null)
            db.SetCompletions.Add(new UserExerciseSetCompletion { UserId = currentUser.UserId, ExerciseSetId = setId });
        else
            db.SetCompletions.Remove(completion);
        await db.SaveChangesAsync(cancellationToken);
        return completion is null;
    }

    public async Task CompleteWorkoutAsync(int workoutId, WorkoutFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<FeedbackDifficulty>(request.Difficulty, true, out var difficulty))
            throw new AppValidationException("Choose a valid workout difficulty.");
        var workout = await db.Workouts.Include(x => x.WorkoutPlan)
            .SingleOrDefaultAsync(x => x.Id == workoutId, cancellationToken)
            ?? throw new AppNotFoundException("Workout not found.");
        if (!workout.WorkoutPlan.IsTemplate && workout.WorkoutPlan.UserId != currentUser.UserId)
            throw new AppForbiddenException("You do not have access to this workout.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var history = await db.WorkoutHistory.SingleOrDefaultAsync(
            x => x.UserId == currentUser.UserId && x.WorkoutId == workoutId && x.CompletedDate == today, cancellationToken);
        if (history is null)
        {
            history = new WorkoutHistory
            {
                UserId = currentUser.UserId, WorkoutId = workout.Id, WorkoutPlanId = workout.WorkoutPlanId,
                CompletedDate = today, PointsAwarded = 25
            };
            db.WorkoutHistory.Add(history);
            var user = await db.Users.SingleAsync(x => x.Id == currentUser.UserId, cancellationToken);
            user.Points += 25;
        }
        history.DurationMinutes = request.DurationMinutes;
        history.FeedbackDifficulty = difficulty;
        history.FeedbackNote = request.Note?.Trim();
        history.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await AchievementService.AwardAsync(db, currentUser.UserId, cancellationToken);
    }

    private static string GoalLabel(FitnessGoal goal) => goal switch
    {
        FitnessGoal.BuildMuscle => "Build Muscle",
        FitnessGoal.LoseFat => "Lose Fat",
        FitnessGoal.ImproveStrength => "Improve Strength",
        _ => "General Fitness"
    };

    private static string RepScheme(FitnessGoal goal, string equipment) => goal switch
    {
        FitnessGoal.ImproveStrength when equipment is "Barbell" or "Machine" => "4–6",
        FitnessGoal.BuildMuscle => "8–12",
        FitnessGoal.LoseFat => "10–15",
        _ => "8–12"
    };

    private static string RestScheme(FitnessGoal goal, string equipment) =>
        goal == FitnessGoal.ImproveStrength && equipment is "Barbell" or "Machine" ? "120–180 sec" :
        goal == FitnessGoal.LoseFat ? "45–60 sec" : "60–90 sec";

    private static IReadOnlyList<Template> Templates(FitnessGoal goal) => goal switch
    {
        FitnessGoal.LoseFat =>
        [
            new("Strength / Density", "Full-body resistance training with purposeful pace.", ["Legs", "Chest", "Back", "Core"], "Stay smooth; keep rests honest."),
            new("Upper Engine", "Upper-body strength balanced with controlled conditioning.", ["Chest", "Back", "Shoulders", "Triceps"], "Leave two clean reps in reserve."),
            new("Lower Engine", "Lower-body strength and trunk control.", ["Legs", "Legs", "Core"], "Finish with an easy ten-minute walk."),
            new("Movement Reset", "Core, pulling, and joint-friendly accessories.", ["Core", "Back", "Biceps", "Shoulders"], "Finish feeling better than you started."),
            new("Full Body Pulse", "Moderate total-body volume with short transitions.", ["Legs", "Chest", "Back", "Core"], "Technique determines the pace."),
            new("Recovery Circuit", "A lighter session to reinforce movement quality.", ["Core", "Shoulders", "Back"], "Use light loads and relaxed breathing.")
        ],
        FitnessGoal.ImproveStrength =>
        [
            new("Squat Signal", "Lower-body strength with long, deliberate rests.", ["Legs", "Legs", "Core"], "Every rep should look repeatable."),
            new("Press Signal", "Pressing strength balanced by upper-back work.", ["Chest", "Back", "Triceps"], "Build load without losing position."),
            new("Hinge Signal", "Posterior-chain strength and bracing.", ["Legs", "Back", "Core"], "Keep the spine neutral and reps crisp."),
            new("Upper Support", "Shoulders, back, and arms supporting the main lifts.", ["Shoulders", "Back", "Biceps", "Triceps"], "Accessories serve the main lifts."),
            new("Technique Lab", "Lower-intensity practice for movement quality.", ["Legs", "Chest", "Back", "Core"], "Move lighter weights perfectly."),
            new("Power Primer", "Low-fatigue compound practice.", ["Legs", "Chest", "Back"], "Stop before speed falls off.")
        ],
        _ =>
        [
            new("Full Body / A", "Balanced strength and movement skill.", ["Legs", "Chest", "Back", "Core"], "Prioritize range and control."),
            new("Full Body / B", "Stable volume and pulling strength.", ["Legs", "Back", "Shoulders", "Biceps"], "Stop before form breaks."),
            new("Full Body / C", "Hinge, press, shoulders, and core.", ["Legs", "Chest", "Shoulders", "Core"], "Add load only to clean reps."),
            new("Upper Volume", "Additional upper-body hypertrophy volume.", ["Chest", "Back", "Shoulders", "Triceps"], "Control the stretch and finish."),
            new("Lower Volume", "Additional leg and trunk work.", ["Legs", "Legs", "Core"], "Keep recovery honest."),
            new("Athletic Reset", "Low-impact movement quality and core work.", ["Core", "Back", "Shoulders"], "Move well and leave fresh.")
        ]
    };

    private sealed record Template(string Title, string Description, string[] Muscles, string Note);
}
