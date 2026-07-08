using MediatR;

namespace RpsArena.Match.Application.Features.Matches.Record;

public sealed record RecordMatchCommand(
    Guid PlayerOneId,
    Guid PlayerTwoId,
    int PlayerOneScore,
    int PlayerTwoScore,
    DateTime PlayedAt,
    Guid? IdempotencyKey) : IRequest<RecordMatchResult>;

/// <summary>
/// Outcome of recording. <see cref="AlreadyExisted"/> distinguishes an
/// idempotent replay (HTTP 200) from a freshly created match (HTTP 201).
/// </summary>
public sealed record RecordMatchResult(MatchDto Match, bool AlreadyExisted);
