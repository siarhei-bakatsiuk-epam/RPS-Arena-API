using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Infrastructure.Persistence.Configurations;

public sealed class PlayerStatsConfiguration : IEntityTypeConfiguration<PlayerStats>
{
    public void Configure(EntityTypeBuilder<PlayerStats> builder)
    {
        builder.ToTable("player_stats");

        builder.HasKey(s => s.PlayerId);
        builder.Property(s => s.PlayerId).ValueGeneratedNever();

        builder.Property(s => s.Username).IsRequired();
        builder.Property(s => s.Wins);
        builder.Property(s => s.Losses);
        builder.Property(s => s.Draws);
        builder.Property(s => s.TotalMatches);
        builder.Property(s => s.MatchPoints);
        builder.Property(s => s.TotalScore);

        // Optimistic concurrency via PostgreSQL's system xmin column (fulfils the
        // ER diagram's rowVersion); no stored column, no extra writes.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Supports the leaderboard ordering (matchPoints desc, totalScore desc).
        builder.HasIndex(s => new { s.MatchPoints, s.TotalScore });
    }
}
