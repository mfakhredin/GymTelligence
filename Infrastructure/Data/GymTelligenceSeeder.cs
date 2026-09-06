using Application.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class GymTelligenceSeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool includeDemoData, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GymTelligenceContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        if (await db.Exercises.AnyAsync(cancellationToken)) return;

        User? demo = null;
        User? trainer = null;
        User? admin = null;
        if (includeDemoData)
        {
            demo = NewUser("Demo User", "demo@gymtelligence.test", UserRole.User, FitnessLevel.Beginner, FitnessGoal.BuildMuscle, 21, 78, 176, 80);
            trainer = NewUser("Trainer Demo", "trainer@gymtelligence.test", UserRole.Trainer, FitnessLevel.Advanced, FitnessGoal.GeneralFitness, 30, 82, 180, 0);
            admin = NewUser("Admin Demo", "admin@gymtelligence.com", UserRole.Admin, FitnessLevel.Advanced, FitnessGoal.ImproveStrength, 35, 84, 182, 0);
            demo.PasswordHash = passwords.Hash(demo, "password");
            trainer.PasswordHash = passwords.Hash(trainer, "trainer123");
            admin.PasswordHash = passwords.Hash(admin, "admin123");
            db.Users.AddRange(demo, trainer, admin);
        }

        var exercises = ExerciseData();
        db.Exercises.AddRange(exercises);
        db.Badges.AddRange(
            new Badge { Slug = "first-workout", Name = "First Signal", Description = "Completed your first workout.", PointsRequired = 20, Icon = "spark" },
            new Badge { Slug = "three-day-streak", Name = "Three in Motion", Description = "Built a three-session rhythm.", PointsRequired = 60, Icon = "streak" },
            new Badge { Slug = "consistency-builder", Name = "Unbreakable", Description = "Completed eight workouts.", PointsRequired = 160, Icon = "shield" },
            new Badge { Slug = "strength-starter", Name = "Load Bearer", Description = "Built early strength consistency.", PointsRequired = 60, Icon = "bolt" },
            new Badge { Slug = "habit-builder", Name = "On Schedule", Description = "Created a training reminder.", PointsRequired = 40, Icon = "clock" });
        db.ScientificSources.AddRange(
            new ScientificSource { Category = "Exercise Guidelines", Name = "American College of Sports Medicine", Description = "Exercise testing and prescription principles.", Url = "https://www.acsm.org" },
            new ScientificSource { Category = "Strength and Conditioning", Name = "National Strength and Conditioning Association", Description = "Strength training, conditioning, and technique education.", Url = "https://www.nsca.com" },
            new ScientificSource { Category = "Public Health", Name = "World Health Organization", Description = "Global physical activity and sedentary behavior guidance.", Url = "https://www.who.int" });

        var plans = new[]
        {
            CreatePlan("Beginner Full Body", FitnessGoal.GeneralFitness, FitnessLevel.Beginner, "A three-day foundation for strong movement patterns.",
                [("Foundation / A", new[] { 7, 1, 3, 12 }), ("Foundation / B", new[] { 9, 4, 5, 11 }), ("Foundation / C", new[] { 8, 2, 6, 13 })], exercises),
            CreatePlan("Push Pull Legs", FitnessGoal.BuildMuscle, FitnessLevel.Intermediate, "A focused hypertrophy split for lifters who recover well.",
                [("Push", new[] { 1, 2, 5, 6, 10 }), ("Pull", new[] { 3, 4, 8, 11 }), ("Legs", new[] { 7, 9, 8, 12 })], exercises),
            CreatePlan("Conditioning Matrix", FitnessGoal.LoseFat, FitnessLevel.Beginner, "Resistance training and efficient conditioning that protects muscle.",
                [("Strength / Density", new[] { 9, 3, 12 }), ("Upper Engine", new[] { 2, 4, 10 }), ("Lower Engine", new[] { 7, 8, 6 }), ("Core / Carry", new[] { 12, 13, 11 })], exercises),
            CreatePlan("Strength Foundation", FitnessGoal.ImproveStrength, FitnessLevel.Intermediate, "Compound strength, progressive overload, and intelligent support work.",
                [("Squat Signal", new[] { 7, 9, 12 }), ("Press Signal", new[] { 1, 4, 10 }), ("Hinge Signal", new[] { 8, 3, 13 }), ("Upper Support", new[] { 5, 6, 11, 12 })], exercises)
        };
        db.WorkoutPlans.AddRange(plans);
        if (demo is not null && trainer is not null && admin is not null)
        {
            db.Subscriptions.AddRange(
                new Subscription { User = demo }, new Subscription { User = trainer }, new Subscription { User = admin });
            db.ProgressEntries.AddRange(
                new ProgressEntry { User = demo, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), Weight = 79.2m, BodyFatPercentage = 22m, WorkoutsCompleted = 1, Notes = "First technique-focused session." },
                new ProgressEntry { User = demo, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)), Weight = 78.8m, BodyFatPercentage = 21.8m, WorkoutsCompleted = 1, Notes = "Better control on rows and squats." },
                new ProgressEntry { User = demo, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), Weight = 78.4m, BodyFatPercentage = 21.6m, WorkoutsCompleted = 1, Notes = "Added a short walk after lifting." },
                new ProgressEntry { User = demo, EntryDate = DateOnly.FromDateTime(DateTime.UtcNow), Weight = 78m, BodyFatPercentage = 21.5m, WorkoutsCompleted = 1, Notes = "Energy felt good today." });
            db.Reminders.Add(new Reminder { User = demo, Title = "Train with controlled form", ReminderTime = new TimeOnly(18, 0), DaysOfWeek = "Mon,Wed,Fri" });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static User NewUser(string name, string email, UserRole role, FitnessLevel level, FitnessGoal goal, int age, decimal weight, decimal height, int points) =>
        new() { Name = name, Email = email, Role = role, FitnessLevel = level, Goal = goal, Age = age, Weight = weight, Height = height, Points = points };

    private static WorkoutPlan CreatePlan(string title, FitnessGoal goal, FitnessLevel level, string description,
        IEnumerable<(string Title, int[] Exercises)> days, IReadOnlyList<Exercise> exercises)
    {
        var plan = new WorkoutPlan { Title = title, Goal = goal, Difficulty = level, Description = description, IsTemplate = true, Source = PlanSource.Seed };
        foreach (var (day, dayIndex) in days.Select((value, index) => (value, index)))
        {
            var workout = new Workout { WorkoutPlan = plan, DayNumber = dayIndex + 1, Title = day.Title, Description = "Technique first. Progress only when every rep stays controlled." };
            plan.Workouts.Add(workout);
            foreach (var (exerciseId, sortIndex) in day.Exercises.Select((value, index) => (value, index)))
            {
                var exercise = exercises[exerciseId - 1];
                var item = new WorkoutExercise { Workout = workout, Exercise = exercise, SortOrder = sortIndex + 1, Notes = "Use a smooth tempo and leave one to two reps in reserve." };
                workout.Exercises.Add(item);
                for (var set = 1; set <= 3; set++)
                    item.Sets.Add(new ExerciseSet { WorkoutExercise = item, SetNumber = set, Reps = exerciseId is 1 or 7 or 8 ? "6–10" : "10–15", RestTime = exerciseId is 1 or 7 or 8 ? "90–150 sec" : "60–90 sec" });
            }
        }
        plan.DaysPerWeek = plan.Workouts.Count;
        return plan;
    }

    private static List<Exercise> ExerciseData()
    {
        var rows = new[]
        {
            (1,"Bench Press","Chest","Pectoralis major","Barbell",FitnessLevel.Intermediate,"A compound press for chest strength and muscle.","Set your shoulder blades, lower to mid-chest, and press with stacked wrists.","Bouncing the bar or losing shoulder position.","Use a spotter or safety arms and warm up gradually.","3–4 × 5–10","Progressive overload and controlled range support strength and hypertrophy."),
            (2,"Incline Dumbbell Press","Chest","Upper pectoralis major","Dumbbell",FitnessLevel.Beginner,"A natural-path press emphasizing the upper chest.","Use a low incline, press smoothly, and lower under control.","Setting the bench too steep or crashing the dumbbells together.","Keep feet planted and choose a controllable load.","3 × 8–12","A low incline increases shoulder flexion demand while preserving chest contribution."),
            (3,"Lat Pulldown","Back","Latissimus dorsi","Cable",FitnessLevel.Beginner,"A vertical pull for lats and upper back.","Drive elbows toward your sides and return under control.","Pulling behind the neck or using momentum.","Avoid yanking the stack and keep the path pain-free.","3–4 × 8–12","Vertical pulling develops shoulder adduction and extension strength."),
            (4,"Seated Cable Row","Back","Mid-back","Cable",FitnessLevel.Beginner,"A horizontal pull for posture and back thickness.","Sit tall, pull toward lower ribs, pause, then extend smoothly.","Rounding the lower back or jerking the torso.","Keep a neutral spine and use a smooth load.","3 × 10–12","Rows train scapular retraction and balance pressing volume."),
            (5,"Shoulder Press","Shoulders","Deltoids","Dumbbell",FitnessLevel.Intermediate,"A vertical press for shoulder strength.","Brace, press overhead without leaning back, and lower with control.","Overarching the lower back or pressing too far forward.","Keep ribs down and stop if shoulder pinching occurs.","3 × 6–10","Vertical pressing loads the deltoids while demanding trunk stability."),
            (6,"Lateral Raise","Shoulders","Lateral deltoid","Dumbbell",FitnessLevel.Beginner,"Controlled isolation work for shoulder width.","Raise slightly forward to shoulder height and lower slowly.","Swinging, shrugging, or choosing excessive weight.","Use light loads and keep the neck relaxed.","2–4 × 12–20","Higher-rep raises load the lateral deltoid without heavy joint stress."),
            (7,"Barbell Squat","Legs","Quadriceps","Barbell",FitnessLevel.Intermediate,"A compound lower-body lift for strength and muscle.","Brace, unlock hips and knees together, descend with control, and drive through mid-foot.","Knee collapse, lost bracing, or rushed depth.","Use safety pins and learn the pattern with lighter loads.","3–5 × 3–8","Squats train knee and hip extension across a large range of motion."),
            (8,"Romanian Deadlift","Legs","Hamstrings","Barbell",FitnessLevel.Intermediate,"A hinge for hamstrings, glutes, and posterior-chain strength.","Push hips back, keep the bar close, and stand by driving hips forward.","Rounding the back or squatting the movement.","Keep the spine neutral and stop at your controllable range.","3–4 × 6–10","Lengthened hamstring loading supports posterior-chain strength and hypertrophy."),
            (9,"Leg Press","Legs","Quadriceps","Machine",FitnessLevel.Beginner,"Stable lower-body volume with low balance demand.","Lower to a comfortable depth and press without hard knee lockout.","Cutting depth or lifting hips from the pad.","Keep the lower back against the pad.","3–4 × 10–15","External stability makes the leg press effective for quad volume."),
            (10,"Cable Triceps Pushdown","Triceps","Triceps brachii","Cable",FitnessLevel.Beginner,"Constant-tension elbow extension.","Keep elbows pinned and control both directions.","Letting elbows drift or leaning heavily.","Use a comfortable grip and pain-free range.","2–4 × 10–15","Cable resistance maintains useful tension through elbow extension."),
            (11,"Dumbbell Curl","Biceps","Biceps brachii","Dumbbell",FitnessLevel.Beginner,"Simple elbow-flexion strength and arm volume.","Stand tall, curl without swinging, and lower slowly.","Torso swing or elbows drifting forward.","Keep wrists neutral and the movement controlled.","2–4 × 8–15","Curls train the elbow flexors through a clear, progressable range."),
            (12,"Plank","Core","Anterior core","Bodyweight",FitnessLevel.Beginner,"An anti-extension drill for trunk stiffness.","Stack ribs and pelvis, brace, and breathe calmly.","Sagging hips or holding your breath.","Stop before form breaks.","3 × 20–45 sec","Planks train the trunk to resist unwanted extension."),
            (13,"Hanging Knee Raise","Core","Abdominals","Bodyweight",FitnessLevel.Intermediate,"Controlled hanging hip flexion and trunk work.","Lift knees without swinging and lower under control.","Using momentum or losing pelvic control.","Use a captain's chair if grip or shoulders are not ready.","2–3 × 8–12","Controlled raises challenge the anterior core and hip flexors.")
        };
        return rows.Select(x => new Exercise { Name = x.Item2, MuscleGroup = x.Item3, TargetMuscle = x.Item4, Equipment = x.Item5, Difficulty = x.Item6, ShortDescription = x.Item7, Instructions = x.Item8, CommonMistakes = x.Item9, SafetyTips = x.Item10, RecommendedSetsReps = x.Item11, ScienceNote = x.Item12 }).ToList();
    }
}
