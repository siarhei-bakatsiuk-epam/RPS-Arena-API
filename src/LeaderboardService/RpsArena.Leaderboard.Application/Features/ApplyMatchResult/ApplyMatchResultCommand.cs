using MediatR;

namespace RpsArena.Leaderboard.Application.Features.ApplyMatchResult;

/// <summary>
/// Applies one recorded match to both players' stats. Issued by the
/// MatchRecorded consumer. <see cref="MessageId"/> is the delivery id used for
/// dedup.
/// </summary>
public sealed record ApplyMatchResultCommand(
    Guid MessageId,
    Guid PlayerOneId,
    string PlayerOneUsername,
    Guid PlayerTwoId,
    string PlayerTwoUsername,
    int PlayerOneScore,
    int PlayerTwoScore) : IRequest;
