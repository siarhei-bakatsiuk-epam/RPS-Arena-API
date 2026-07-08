using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.Api.Middleware;

/// <summary>
/// Translates application exceptions into RFC 7807 ProblemDetails responses:
/// ValidationException -> 400 (+ per-field errors), NotFoundException -> 404,
/// ConflictException -> 409, everything else -> 500 (message not leaked).
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("{Status} on {Path}: {Message}", status, httpContext.Request.Path, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.io/{status}",
            Detail = status == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred while processing your request."
                : exception.Message,
        };

        if (exception is ValidationException validationException)
        {
            problemDetails.Detail = "One or more validation errors occurred.";
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
    }
}
