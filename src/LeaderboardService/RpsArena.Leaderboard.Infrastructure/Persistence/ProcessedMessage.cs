namespace RpsArena.Leaderboard.Infrastructure.Persistence;

/// <summary>
/// Consumer dedup marker. A row is inserted in the same transaction as the stats
/// update; the PK on message_id makes a redelivered event a no-op (exactly-once
/// effect over at-least-once delivery).
/// </summary>
public sealed class ProcessedMessage
{
    public Guid MessageId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid messageId, DateTime processedAtUtc)
    {
        MessageId = messageId;
        ProcessedAt = processedAtUtc;
    }
}
