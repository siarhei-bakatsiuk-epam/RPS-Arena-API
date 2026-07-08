using Microsoft.EntityFrameworkCore;
using RpsArena.Leaderboard.Application.Common.Abstractions;

namespace RpsArena.Leaderboard.Infrastructure.Persistence.Repositories;

public sealed class ProcessedMessageStore(LeaderboardDbContext context) : IProcessedMessageStore
{
    public Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        context.ProcessedMessages.AnyAsync(m => m.MessageId == messageId, cancellationToken);

    public void Add(Guid messageId) =>
        context.ProcessedMessages.Add(new ProcessedMessage(messageId, DateTime.UtcNow));
}
