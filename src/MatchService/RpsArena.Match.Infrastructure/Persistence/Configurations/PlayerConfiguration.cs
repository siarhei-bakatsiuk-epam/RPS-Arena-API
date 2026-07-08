using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Username)
            .HasMaxLength(32)
            .IsRequired();

        // citext => case-insensitive comparisons, so the unique index is case-insensitive.
        builder.Property(p => p.Email)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(p => p.Username).IsUnique();
        builder.HasIndex(p => p.Email).IsUnique();
    }
}
