using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RpsArena.Match.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build the model and generate
/// migrations without the API host being wired up (that happens in Step 4).
/// </summary>
public sealed class MatchDbContextFactory : IDesignTimeDbContextFactory<MatchDbContext>
{
    public MatchDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=match_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MatchDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(MatchDbContextFactory).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MatchDbContext(options);
    }
}
