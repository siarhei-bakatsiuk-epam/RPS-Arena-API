using MediatR;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Application.Common.Exceptions;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Application.Features.ApplyMatchResult;

public sealed class ApplyMatchResultHandler(
    IPlayerStatsRepository stats,
    IProcessedMessageStore processedMessages,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApplyMatchResultCommand>
{
    public async Task Handle(ApplyMatchResultCommand request, CancellationToken cancellationToken)
    {
        // Fast dedup: already applied.
        if (await processedMessages.ExistsAsync(request.MessageId, cancellationToken))
        {
            return;
        }

        // One transaction: the dedup marker + both players' stats. SaveChanges
        // wraps all of it, so partial application is impossible.
        processedMessages.Add(request.MessageId);

        await ApplyForPlayerAsync(
            request.PlayerOneId, request.PlayerOneUsername,
            request.PlayerOneScore, request.PlayerTwoScore, cancellationToken);

        await ApplyForPlayerAsync(
            request.PlayerTwoId, request.PlayerTwoUsername,
            request.PlayerTwoScore, request.PlayerOneScore, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateMessageException)
        {
            // A concurrent redelivery won the race and already applied this
            // message; our changes rolled back. Nothing more to do.
        }
        // ConcurrencyConflictException intentionally propagates -> MassTransit
        // redelivers and the retry re-reads fresh state.
    }

    private async Task ApplyForPlayerAsync(
        Guid playerId, string username, int playerScore, int opponentScore, CancellationToken cancellationToken)
    {
        var playerStats = await stats.GetByIdAsync(playerId, cancellationToken);

        if (playerStats is null)
        {
            playerStats = PlayerStats.Create(playerId, username);
            await stats.AddAsync(playerStats, cancellationToken);
        }

        playerStats.ApplyResult(playerScore, opponentScore, username);
    }
}
