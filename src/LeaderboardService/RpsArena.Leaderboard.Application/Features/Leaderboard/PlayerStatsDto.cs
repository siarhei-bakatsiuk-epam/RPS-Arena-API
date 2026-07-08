namespace RpsArena.Leaderboard.Application.Features.Leaderboard;

/// <summary>
/// Player standing including the computed <see cref="Rank"/> (DENSE_RANK over
/// match_points desc, total_score desc). Rank is never stored.
/// </summary>
public sealed record PlayerStatsDto(
    int Rank,
    Guid PlayerId,
    string Username,
    int Wins,
    int Losses,
    int Draws,
    int TotalMatches,
    int MatchPoints,
    int TotalScore);
