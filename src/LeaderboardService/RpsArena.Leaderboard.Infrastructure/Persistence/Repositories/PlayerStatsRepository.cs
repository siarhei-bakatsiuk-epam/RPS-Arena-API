using Microsoft.EntityFrameworkCore;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Infrastructure.Persistence.Repositories;

public sealed class PlayerStatsRepository(LeaderboardDbContext context) : IPlayerStatsRepository
{
    // Tracked (not AsNoTracking): ApplyResult mutations must be detected and the
    // xmin concurrency token checked on SaveChanges.
    public Task<PlayerStats?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        context.PlayerStats.FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken);

    public async Task AddAsync(PlayerStats stats, CancellationToken cancellationToken = default) =>
        await context.PlayerStats.AddAsync(stats, cancellationToken);
}
