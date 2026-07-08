using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RpsArena.Match.Domain.Entities;
using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<MatchEntity>
{
    public void Configure(EntityTypeBuilder<MatchEntity> builder)
    {
        builder.ToTable("matches", table =>
        {
            table.HasCheckConstraint(
                "ck_matches_scores_non_negative",
                "player_one_score >= 0 AND player_two_score >= 0");
            table.HasCheckConstraint(
                "ck_matches_no_self_play",
                "player_one_id <> player_two_id");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.PlayerOneScore).IsRequired();
        builder.Property(m => m.PlayerTwoScore).IsRequired();
        builder.Property(m => m.PlayedAt).IsRequired();
        builder.Property(m => m.IdempotencyKey).IsRequired();

        // Idempotency guard: at most one match per key.
        builder.HasIndex(m => m.IdempotencyKey).IsUnique();

        // Filter/query indexes.
        builder.HasIndex(m => m.PlayerOneId);
        builder.HasIndex(m => m.PlayerTwoId);
        builder.HasIndex(m => m.PlayedAt);

        // FKs to players; no navigations. RESTRICT keeps match history consistent
        // (a player with matches cannot be deleted).
        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(m => m.PlayerOneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(m => m.PlayerTwoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
