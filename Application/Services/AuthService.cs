using Application.Common;
using Application.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Application.Services;

public sealed class AuthService(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IPasswordService passwords,
    ITokenService tokens,
    IEmailService emails) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new AppValidationException("An account with this email already exists.");

        if (!Enum.TryParse<FitnessLevel>(request.FitnessLevel, true, out var level) ||
            !Enum.TryParse<FitnessGoal>(request.Goal.Replace(" ", string.Empty), true, out var goal))
            throw new AppValidationException("Choose a valid fitness level and goal.");

        ValidatePassword(request.Password);
        var user = new User
        {
            Name = request.Name.Trim(), Email = email, Age = request.Age,
            Weight = request.Weight, Height = request.Height,
            FitnessLevel = level, Goal = goal
        };
        user.PasswordHash = passwords.Hash(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(tokens.Create(user), user.ToResponse());
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive || !passwords.Verify(user, user.PasswordHash, request.Password))
            throw new AppValidationException("The email or password is incorrect.");
        return new AuthResponse(tokens.Create(user), user.ToResponse());
    }

    public async Task<UserResponse> MeAsync(CancellationToken cancellationToken = default) =>
        (await GetCurrentAsync(cancellationToken)).ToResponse();

    public async Task<UserResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentAsync(cancellationToken);
        if (!Enum.TryParse<FitnessLevel>(request.FitnessLevel, true, out var level) ||
            !Enum.TryParse<FitnessGoal>(request.Goal.Replace(" ", string.Empty), true, out var goal))
            throw new AppValidationException("Choose a valid fitness level and goal.");

        user.Name = request.Name.Trim();
        user.Age = request.Age;
        user.Weight = request.Weight;
        user.Height = request.Height;
        user.FitnessLevel = level;
        user.Goal = goal;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return user.ToResponse();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentAsync(cancellationToken);
        if (!passwords.Verify(user, user.PasswordHash, request.CurrentPassword))
            throw new AppValidationException("Your current password is incorrect.");
        ValidatePassword(request.NewPassword);
        user.PasswordHash = passwords.Hash(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string publicBaseUrl,
        bool includeDevelopmentLink,
        CancellationToken cancellationToken = default)
    {
        const string genericMessage = "If that email exists, a password reset link has been prepared.";
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
        if (user is null)
            return new ForgotPasswordResponse(genericMessage);

        var activeTokens = await db.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAt == null)
            .ToListAsync(cancellationToken);
        db.PasswordResetTokens.RemoveRange(activeTokens);

        var selector = Convert.ToHexString(RandomNumberGenerator.GetBytes(9)).ToLowerInvariant();
        var validator = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Selector = selector,
            TokenHash = HashResetToken(validator),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await db.SaveChangesAsync(cancellationToken);

        var relativeUrl = $"/reset-password?selector={Uri.EscapeDataString(selector)}&validator={Uri.EscapeDataString(validator)}";
        var absoluteUrl = $"{publicBaseUrl.TrimEnd('/')}{relativeUrl}";
        if (emails.IsConfigured)
            await emails.SendPasswordResetAsync(user.Email, user.Name, absoluteUrl, cancellationToken);
        return new ForgotPasswordResponse(genericMessage, includeDevelopmentLink ? relativeUrl : null);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePassword(request.NewPassword);
        var token = await db.PasswordResetTokens
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Selector == request.Selector && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (token is null || !FixedTimeEquals(token.TokenHash, HashResetToken(request.Validator)))
            throw new AppValidationException("This reset link is invalid or expired.");
        if (!token.User.IsActive)
            throw new AppValidationException("This account is unavailable.");

        token.User.PasswordHash = passwords.Hash(token.User, request.NewPassword);
        token.User.UpdatedAt = DateTime.UtcNow;
        token.UsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetCurrentAsync(CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(x => x.Id == currentUser.UserId && x.IsActive, cancellationToken)
        ?? throw new AppForbiddenException("Your account is unavailable.");

    private static void ValidatePassword(string value)
    {
        if (value.Length < 8 || !value.Any(char.IsLetter) || !value.Any(char.IsDigit))
            throw new AppValidationException("Password must be at least 8 characters and include a letter and a number.");
    }

    private static string HashResetToken(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
