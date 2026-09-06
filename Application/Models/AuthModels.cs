using System.ComponentModel.DataAnnotations;

namespace Application.Models;

public sealed class RegisterRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(190)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = string.Empty;
    [Range(13, 100)] public int Age { get; set; } = 18;
    [Range(20, 400)] public decimal Weight { get; set; } = 70;
    [Range(90, 250)] public decimal Height { get; set; } = 170;
    [Required] public string FitnessLevel { get; set; } = "Beginner";
    [Required] public string Goal { get; set; } = "GeneralFitness";
}

public sealed class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public sealed record AuthResponse(string Token, UserResponse User);

public sealed record UserResponse(
    int Id, string Name, string Email, string Role, int? Age, decimal? Weight,
    decimal? Height, string FitnessLevel, string Goal, string SubscriptionStatus,
    int Points, DateTime CreatedAt, bool IsActive);

public sealed class UpdateProfileRequest
{
    [Required, StringLength(120, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Range(13, 100)] public int Age { get; set; }
    [Range(20, 400)] public decimal Weight { get; set; }
    [Range(90, 250)] public decimal Height { get; set; }
    [Required] public string FitnessLevel { get; set; } = "Beginner";
    [Required] public string Goal { get; set; } = "GeneralFitness";
}

public sealed class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(190)] public string Email { get; set; } = string.Empty;
}

public sealed record ForgotPasswordResponse(string Message, string? DevelopmentResetUrl = null);

public sealed class ResetPasswordRequest
{
    [Required, StringLength(40, MinimumLength = 18)] public string Selector { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 64)] public string Validator { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(100)] public string NewPassword { get; set; } = string.Empty;
}
