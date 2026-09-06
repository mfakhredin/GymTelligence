using System.Security.Claims;
using Application.Common;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal User => accessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("Authentication is required.");

    public int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new UnauthorizedAccessException("User identity is missing.");

    public string Role => User.FindFirstValue(ClaimTypes.Role)
        ?? throw new UnauthorizedAccessException("User role is missing.");
}
