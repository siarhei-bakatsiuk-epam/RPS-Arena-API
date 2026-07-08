using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;
using RpsArena.Match.Infrastructure.Persistence;

namespace RpsArena.Match.Infrastructure;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations on startup, retrying while PostgreSQL
    /// is still coming up (compose brings the DB and API up together). This is
    /// the functional equivalent of the spec's entrypoint `dotnet ef database
    /// update`, without an extra init container.
    /// </summary>
    public static async Task MigrateMatchDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("MatchService.Migrations");

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(3),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder()
                    .Handle<NpgsqlException>()
                    .Handle<SocketException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Database not ready (attempt {Attempt}); retrying in {Delay}.",
                        args.AttemptNumber + 1, args.RetryDelay);
                    return default;
                }
            })
            .Build();

        await pipeline.ExecuteAsync(async ct =>
        {
            logger.LogInformation("Applying MatchService database migrations...");
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("MatchService database migrations applied.");
        }, cancellationToken);
    }
}
