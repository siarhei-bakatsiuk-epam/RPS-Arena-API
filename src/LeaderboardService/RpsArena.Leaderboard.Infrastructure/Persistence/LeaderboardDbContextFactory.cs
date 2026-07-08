using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RpsArena.Leaderboard.Infrastructure.Persistence;

/// <summary>Design-time factory so <c>dotnet ef</c> can generate migrations.</summary>
public sealed class LeaderboardDbContextFactory : IDesignTimeDbContextFactory<LeaderboardDbContext>
{
    public LeaderboardDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=leaderboard_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<LeaderboardDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(LeaderboardDbContextFactory).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LeaderboardDbContext(options);
    }
}
