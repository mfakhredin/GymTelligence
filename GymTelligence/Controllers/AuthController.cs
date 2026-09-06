using Application.Models;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymTelligence.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService service, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("authentication")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) => Ok(await service.RegisterAsync(request, ct));

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("authentication")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) => Ok(await service.LoginAsync(request, ct));

    [HttpPost("forgot-password"), AllowAnonymous, EnableRateLimiting("authentication")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct) =>
        Ok(await service.ForgotPasswordAsync(request, $"{Request.Scheme}://{Request.Host}", environment.IsDevelopment(), ct));

    [HttpPost("reset-password"), AllowAnonymous, EnableRateLimiting("authentication")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(request, ct);
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken ct) => Ok(await service.MeAsync(ct));

    [HttpPut("profile"), Authorize]
    public async Task<ActionResult<UserResponse>> Profile(UpdateProfileRequest request, CancellationToken ct) => Ok(await service.UpdateProfileAsync(request, ct));

    [HttpPut("password"), Authorize]
    public async Task<IActionResult> Password(ChangePasswordRequest request, CancellationToken ct)
    {
        await service.ChangePasswordAsync(request, ct);
        return NoContent();
    }
}
