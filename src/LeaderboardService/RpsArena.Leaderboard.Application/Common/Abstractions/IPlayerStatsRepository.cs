using RpsArena.Leaderboard.Application.Features.Leaderboard;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Application.Common.Abstractions;

public interface IPlayerStatsRepository
{
    Task<PlayerStats?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task AddAsync(PlayerStats stats, CancellationToken cancellationToken = default);

    /// <summary>Top-N players ordered by <paramref name="sortBy"/> with a
    /// deterministic tie-break, each carrying the computed DENSE_RANK.</summary>
    Task<IReadOnlyList<PlayerStatsDto>> GetLeaderboardAsync(
        LeaderboardSortBy sortBy, int top, CancellationToken cancellationToken = default);

    /// <summary>A single player's stats including the computed rank, or null.</summary>
    Task<PlayerStatsDto?> GetRankedByIdAsync(Guid playerId, CancellationToken cancellationToken = default);
}
