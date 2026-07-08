using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.Application.Common.Abstractions;

public interface IMatchRepository
{
    Task<MatchEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MatchEntity?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey, CancellationToken cancellationToken = default);

    Task AddAsync(MatchEntity match, CancellationToken cancellationToken = default);
}
