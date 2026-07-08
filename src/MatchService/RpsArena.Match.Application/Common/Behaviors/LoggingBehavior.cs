using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RpsArena.Match.Application.Common.Behaviors;

/// <summary>
/// Structured request/response logging with elapsed time for every MediatR
/// request. Kept free of ASP.NET dependencies so the Application layer stays
/// framework-agnostic.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                ex,
                "{RequestName} failed after {ElapsedMilliseconds} ms: {Message}",
                requestName, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
