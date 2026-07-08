namespace RpsArena.Leaderboard.Domain.Entities;

/// <summary>
/// Accumulated per-player leaderboard statistics. Aggregate root for the
/// player_stats table. All mutation goes through <see cref="ApplyResult"/> so the
/// scoring rules live in one place.
/// </summary>
public class PlayerStats
{
    public const int WinPoints = 3;
    public const int DrawPoints = 1;
    public const int LossPoints = 0;

    public Guid PlayerId { get; private set; }
    public string Username { get; private set; } = null!;
    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int Draws { get; private set; }
    public int TotalMatches { get; private set; }
    public int MatchPoints { get; private set; }
    public int TotalScore { get; private set; }

    // Required by EF Core materialization.
    private PlayerStats()
    {
    }

    private PlayerStats(Guid playerId, string username)
    {
        PlayerId = playerId;
        Username = username;
    }

    public static PlayerStats Create(Guid playerId, string username) => new(playerId, username);

    /// <summary>Classifies a result from the perspective of one player.</summary>
    public static MatchOutcome Classify(int playerScore, int opponentScore) =>
        playerScore > opponentScore ? MatchOutcome.Win
        : playerScore < opponentScore ? MatchOutcome.Loss
        : MatchOutcome.Draw;

    public static int PointsFor(MatchOutcome outcome) => outcome switch
    {
        MatchOutcome.Win => WinPoints,
        MatchOutcome.Draw => DrawPoints,
        _ => LossPoints,
    };

    /// <summary>
    /// Applies one match to this player's stats along both axes: outcome
    /// (win/draw/loss -> matchPoints 3/1/0) and rounds won (totalScore). The
    /// latest username is carried through so renames eventually propagate.
    /// </summary>
    public void ApplyResult(int playerScore, int opponentScore, string currentUsername)
    {
        Username = currentUsername;

        var outcome = Classify(playerScore, opponentScore);
        switch (outcome)
        {
            case MatchOutcome.Win:
                Wins++;
                break;
            case MatchOutcome.Draw:
                Draws++;
                break;
            case MatchOutcome.Loss:
                Losses++;
                break;
        }

        MatchPoints += PointsFor(outcome);
        TotalScore += playerScore;
        TotalMatches++;
    }
}
