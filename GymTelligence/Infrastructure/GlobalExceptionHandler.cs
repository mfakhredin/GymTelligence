using Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GymTelligence.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            AppNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            AppForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication is required."),
            _ => (StatusCodes.Status500InternalServerError, "Something unexpected happened.")
        };
        if (status == 500) logger.LogError(exception, "Unhandled request error");
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title,
            Detail = status == 500 ? "Please try again. If the problem continues, check the server logs." : null,
            Instance = context.Request.Path
        }, cancellationToken);
        return true;
    }
}
