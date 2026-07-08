namespace RpsArena.Leaderboard.Application.Features.Leaderboard;

public enum LeaderboardSortBy
{
    MatchPoints,
    Wins,
    Draws,
    Losses,
    TotalScore,
}

public static class LeaderboardSortByParser
{
    public static bool TryParse(string? value, out LeaderboardSortBy sortBy)
    {
        sortBy = LeaderboardSortBy.MatchPoints;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true; // default
        }

        return Enum.TryParse(value, ignoreCase: true, out sortBy);
    }
}
