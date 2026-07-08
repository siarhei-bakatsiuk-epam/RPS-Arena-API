using MassTransit;
using Microsoft.EntityFrameworkCore;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.Infrastructure.Persistence;

public class MatchDbContext(DbContextOptions<MatchDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Domain.Entities.Match> Matches => Set<Domain.Entities.Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // citext enables case-insensitive unique email without a functional index.
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchDbContext).Assembly);

        // MassTransit transactional outbox tables (InboxState/OutboxState/
        // OutboxMessage) so publishing the event commits atomically with the match.
        modelBuilder.AddTransactionalOutboxEntities();
    }
}
