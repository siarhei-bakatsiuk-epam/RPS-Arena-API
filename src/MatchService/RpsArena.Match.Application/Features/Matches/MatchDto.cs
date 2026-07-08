using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.Application.Features.Matches;

public sealed record MatchDto(
    Guid Id,
    Guid PlayerOneId,
    Guid PlayerTwoId,
    int PlayerOneScore,
    int PlayerTwoScore,
    DateTime PlayedAt,
    Guid IdempotencyKey)
{
    public static MatchDto FromEntity(MatchEntity m) =>
        new(m.Id, m.PlayerOneId, m.PlayerTwoId, m.PlayerOneScore, m.PlayerTwoScore, m.PlayedAt, m.IdempotencyKey);
}
