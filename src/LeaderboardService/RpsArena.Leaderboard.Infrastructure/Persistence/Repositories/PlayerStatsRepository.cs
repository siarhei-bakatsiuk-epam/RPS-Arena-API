using Microsoft.EntityFrameworkCore;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Application.Features.Leaderboard;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.Infrastructure.Persistence.Repositories;

public sealed class PlayerStatsRepository(LeaderboardDbContext context) : IPlayerStatsRepository
{
    // Rank is always DENSE_RANK over (match_points desc, total_score desc) — the
    // canonical tournament standing, independent of the list sort order. Columns
    // stay snake_case so EF's snake_case convention maps them to the DTO
    // properties; only the computed rank needs an explicit alias.
    private const string RankedProjection = """
        SELECT DENSE_RANK() OVER (ORDER BY match_points DESC, total_score DESC)::int AS rank,
               player_id, username, wins, losses, draws, total_matches, match_points, total_score
        FROM player_stats
        """;

    // Tracked (not AsNoTracking): ApplyResult mutations must be detected and the
    // xmin concurrency token checked on SaveChanges.
    public Task<PlayerStats?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        context.PlayerStats.FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken);

    public async Task AddAsync(PlayerStats stats, CancellationToken cancellationToken = default) =>
        await context.PlayerStats.AddAsync(stats, cancellationToken);

    public async Task<IReadOnlyList<PlayerStatsDto>> GetLeaderboardAsync(
        LeaderboardSortBy sortBy, int top, CancellationToken cancellationToken = default)
    {
        // sortColumn is a fixed whitelist value and top is a validated int
        // (1..100) -> safe to interpolate; no user text reaches the SQL.
        var sortColumn = MapSortColumn(sortBy);
        var sql = $"""
            {RankedProjection}
            ORDER BY {sortColumn} DESC, total_score DESC, username ASC
            LIMIT {top}
            """;

        return await context.Database
            .SqlQueryRaw<PlayerStatsDto>(sql)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlayerStatsDto?> GetRankedByIdAsync(
        Guid playerId, CancellationToken cancellationToken = default)
    {
        // playerId is parameterized ({0} -> @p0) rather than interpolated.
        var sql = "SELECT * FROM (" + RankedProjection + "\n) ranked WHERE player_id = {0}";

        return await context.Database
            .SqlQueryRaw<PlayerStatsDto>(sql, playerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string MapSortColumn(LeaderboardSortBy sortBy) => sortBy switch
    {
        LeaderboardSortBy.Wins => "wins",
        LeaderboardSortBy.Draws => "draws",
        LeaderboardSortBy.Losses => "losses",
        LeaderboardSortBy.TotalScore => "total_score",
        _ => "match_points",
    };
}
