using Domain.Entities;
using Application.Models;

namespace Application.Common;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
}

public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string hash, string password);
}

public interface ITokenService
{
    string Create(User user);
}

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendPasswordResetAsync(string recipientEmail, string recipientName, string resetUrl, CancellationToken cancellationToken = default);
}

public interface IAiProvider
{
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);
}

public interface IBackupService
{
    Task<BackupResponse> CreateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupResponse>> RecentAsync(CancellationToken cancellationToken = default);
}

public sealed class AppValidationException(string message) : Exception(message);
public sealed class AppNotFoundException(string message) : Exception(message);
public sealed class AppForbiddenException(string message) : Exception(message);
