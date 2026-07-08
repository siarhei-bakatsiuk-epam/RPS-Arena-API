using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RpsArena.Leaderboard.Infrastructure.Persistence.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");

        builder.HasKey(m => m.MessageId);
        builder.Property(m => m.MessageId).ValueGeneratedNever();
        builder.Property(m => m.ProcessedAt).IsRequired();
    }
}
