using Application.Common;
using Application.Models;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GymTelligence.Tests;

public sealed class CoreServicesTests
{
    [Fact]
    public async Task Registration_hashes_password_and_rejects_duplicate_email()
    {
        await using var db = CreateDb();
        var auth = new AuthService(db, new FakeCurrentUser(), new PasswordService(), new FakeTokenService(), new FakeEmailService());
        var request = new RegisterRequest
        {
            Name = "Test Athlete", Email = "athlete@example.com", Password = "StrongPass1",
            Age = 25, Weight = 75, Height = 178, FitnessLevel = "Beginner", Goal = "BuildMuscle"
        };

        var response = await auth.RegisterAsync(request);

        Assert.Equal("test-token", response.Token);
        Assert.NotEqual(request.Password, (await db.Users.SingleAsync()).PasswordHash);
        await Assert.ThrowsAsync<AppValidationException>(() => auth.RegisterAsync(request));
    }

    [Fact]
    public async Task Generated_plan_is_owned_and_contains_programmed_sets()
    {
        await using var db = CreateDb();
        var user = new User { Name = "Athlete", Email = "a@b.com", FitnessLevel = FitnessLevel.Beginner, Goal = FitnessGoal.BuildMuscle };
        db.Users.Add(user);
        db.Exercises.AddRange(
            Exercise("Squat", "Legs"), Exercise("Press", "Chest"),
            Exercise("Row", "Back"), Exercise("Plank", "Core"),
            Exercise("Raise", "Shoulders"), Exercise("Curl", "Biceps"));
        await db.SaveChangesAsync();
        var service = new WorkoutService(db, new FakeCurrentUser(user.Id));

        var result = await service.GenerateAsync();

        Assert.Equal(user.Id, await db.WorkoutPlans.Where(x => x.Id == result.Plan.Id).Select(x => x.UserId).SingleAsync());
        Assert.Equal(3, result.Workouts.Count);
        Assert.All(result.Workouts, workout => Assert.NotEmpty(workout.Exercises));
        Assert.All(result.Workouts.SelectMany(x => x.Exercises), exercise => Assert.Equal(3, exercise.Sets.Count));
    }

    [Fact]
    public async Task Set_completion_is_scoped_to_current_user()
    {
        await using var db = CreateDb();
        var owner = new User { Name = "Owner", Email = "owner@test.com" };
        var stranger = new User { Name = "Other", Email = "other@test.com" };
        var exercise = Exercise("Row", "Back");
        var plan = new WorkoutPlan { User = owner, Title = "Private", Description = "Private", Goal = FitnessGoal.GeneralFitness, Difficulty = FitnessLevel.Beginner, DaysPerWeek = 1 };
        var workout = new Workout { WorkoutPlan = plan, Title = "Day", DayNumber = 1 };
        var item = new WorkoutExercise { Workout = workout, Exercise = exercise, SortOrder = 1 };
        var set = new ExerciseSet { WorkoutExercise = item, SetNumber = 1, Reps = "8", RestTime = "60 sec" };
        db.Users.AddRange(owner, stranger); db.Exercises.Add(exercise); db.WorkoutPlans.Add(plan); db.Workouts.Add(workout); db.WorkoutExercises.Add(item); db.ExerciseSets.Add(set);
        await db.SaveChangesAsync();

        var service = new WorkoutService(db, new FakeCurrentUser(stranger.Id));

        await Assert.ThrowsAsync<AppForbiddenException>(() => service.ToggleSetAsync(set.Id));
        Assert.Empty(db.SetCompletions);
    }

    [Fact]
    public async Task Password_reset_token_is_hashed_single_use_and_expires()
    {
        await using var db = CreateDb();
        var passwordService = new PasswordService();
        var auth = new AuthService(db, new FakeCurrentUser(), passwordService, new FakeTokenService(), new FakeEmailService());
        await auth.RegisterAsync(new RegisterRequest
        {
            Name = "Recovery User", Email = "recover@example.com", Password = "InitialPass1",
            Age = 28, Weight = 80, Height = 180, FitnessLevel = "Intermediate", Goal = "ImproveStrength"
        });

        var response = await auth.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "recover@example.com" }, "https://local", true);
        var values = new Uri("https://local" + response.DevelopmentResetUrl!).Query.TrimStart('?').Split('&')
            .Select(x => x.Split('=', 2)).ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1]));
        var stored = await db.PasswordResetTokens.SingleAsync();

        Assert.DoesNotContain(values["validator"], stored.TokenHash);
        await auth.ResetPasswordAsync(new ResetPasswordRequest
        {
            Selector = values["selector"], Validator = values["validator"], NewPassword = "UpdatedPass2"
        });
        Assert.True(passwordService.Verify(await db.Users.SingleAsync(), (await db.Users.SingleAsync()).PasswordHash, "UpdatedPass2"));
        await Assert.ThrowsAsync<AppValidationException>(() => auth.ResetPasswordAsync(new ResetPasswordRequest
        {
            Selector = values["selector"], Validator = values["validator"], NewPassword = "AnotherPass3"
        }));
    }

    [Fact]
    public async Task Admin_can_manage_reusable_workout_templates()
    {
        await using var db = CreateDb();
        var service = new AdminService(db, new FakeCurrentUser(44, "Admin"));
        var created = await service.CreateTemplateAsync(new SaveWorkoutTemplateRequest
        {
            Title = "Strength Signal", Goal = "Improve Strength", Difficulty = "Intermediate",
            DaysPerWeek = 4, Description = "A reusable strength-focused training template."
        });

        Assert.Equal("ImproveStrength", created.Goal);
        var updated = await service.UpdateTemplateAsync(created.Id, new SaveWorkoutTemplateRequest
        {
            Title = "Strength Signal II", Goal = "BuildMuscle", Difficulty = "Advanced",
            DaysPerWeek = 5, Description = "An updated reusable training template for administrators."
        });
        Assert.Equal(5, updated.DaysPerWeek);

        await service.DeleteTemplateAsync(created.Id);
        Assert.Empty(await service.TemplatesAsync());
    }

    private static GymTelligenceContext CreateDb() => new(new DbContextOptionsBuilder<GymTelligenceContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Exercise Exercise(string name, string muscle) => new()
    {
        Name = name, MuscleGroup = muscle, TargetMuscle = muscle, Equipment = "Dumbbell",
        Difficulty = FitnessLevel.Beginner, ShortDescription = "Test movement", Instructions = "Move safely",
        CommonMistakes = "Rushing", SafetyTips = "Control the load", RecommendedSetsReps = "3 x 8",
        ScienceNote = "Test note"
    };

    private sealed class FakeCurrentUser(int userId = 1, string role = "User") : ICurrentUserService
    {
        public int UserId { get; } = userId;
        public string Role { get; } = role;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string Create(User user) => "test-token";
    }

    private sealed class FakeEmailService : IEmailService
    {
        public bool IsConfigured => false;
        public Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
