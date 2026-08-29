using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StartupIMS.API.Middleware;

/// <summary>
/// Catches any unhandled exception anywhere in the request pipeline.
/// Always logs the full exception server-side. Only returns the exception's
/// details to the client when running in Development - in any other
/// environment, the client gets a generic message, since stack
/// traces/internal type names are an information-disclosure risk if leaked
/// to an end user (file paths, library versions, internal class names).
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = httpContext.Request.Path
        };

        if (_env.IsDevelopment())
        {
            // Safe to expose locally - helps debugging, never reaches a real user.
            problemDetails.Detail = exception.ToString();
        }
        else
        {
            problemDetails.Detail = "Something went wrong on our end. Please try again, and contact support if the problem continues.";
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // tells ASP.NET Core the exception has been handled
    }
}
