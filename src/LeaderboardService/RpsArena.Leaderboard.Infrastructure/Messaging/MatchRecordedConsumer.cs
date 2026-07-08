using MassTransit;
using MediatR;
using RpsArena.Contracts;
using RpsArena.Leaderboard.Application.Features.ApplyMatchResult;

namespace RpsArena.Leaderboard.Infrastructure.Messaging;

/// <summary>
/// Consumes MatchRecorded and applies it via the ApplyMatchResult command. The
/// broker delivery id (context.MessageId) is the dedup key. A thrown
/// ConcurrencyConflictException propagates so MassTransit's retry policy
/// redelivers; after retries are exhausted the message lands in the _error queue.
/// </summary>
public sealed class MatchRecordedConsumer(ISender mediator) : IConsumer<MatchRecorded>
{
    public Task Consume(ConsumeContext<MatchRecorded> context)
    {
        var message = context.Message;
        var messageId = context.MessageId ?? message.MatchId;

        return mediator.Send(
            new ApplyMatchResultCommand(
                messageId,
                message.PlayerOneId, message.PlayerOneUsername,
                message.PlayerTwoId, message.PlayerTwoUsername,
                message.PlayerOneScore, message.PlayerTwoScore),
            context.CancellationToken);
    }
}
