using Microsoft.EntityFrameworkCore;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Infrastructure.Persistence;

public class LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options) : DbContext(options)
{
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeaderboardDbContext).Assembly);
    }
}
