using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Application.Common.Abstractions;

public interface IPlayerStatsRepository
{
    Task<PlayerStats?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task AddAsync(PlayerStats stats, CancellationToken cancellationToken = default);
}
