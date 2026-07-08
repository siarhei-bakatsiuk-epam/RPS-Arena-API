namespace RpsArena.Leaderboard.Application.Common.Abstractions;

/// <summary>Consumer dedup store (backed by the processed_messages table).</summary>
public interface IProcessedMessageStore
{
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Enrolls the message id for insertion on the next SaveChanges.</summary>
    void Add(Guid messageId);
}
