using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;
using RpsArena.Leaderboard.Infrastructure.Persistence;

namespace RpsArena.Leaderboard.Infrastructure;

public static class MigrationExtensions
{
    /// <summary>Applies pending migrations on startup, retrying while PostgreSQL comes up.</summary>
    public static async Task MigrateLeaderboardDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LeaderboardDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("LeaderboardService.Migrations");

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
            logger.LogInformation("Applying LeaderboardService database migrations...");
            await db.Database.MigrateAsync(ct);
            logger.LogInformation("LeaderboardService database migrations applied.");
        }, cancellationToken);
    }
}
