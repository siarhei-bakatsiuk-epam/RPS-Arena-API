using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RpsArena.Leaderboard.Infrastructure.Persistence;

namespace RpsArena.Leaderboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLeaderboardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured.");

        services.AddDbContext<LeaderboardDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(LeaderboardDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            })
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
