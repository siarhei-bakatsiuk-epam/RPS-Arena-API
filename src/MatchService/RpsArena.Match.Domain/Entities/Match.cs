namespace RpsArena.Match.Domain.Entities;

/// <summary>
/// A completed head-to-head match (several RPS rounds). Scores are the rounds
/// won by each player. Immutable once recorded.
/// </summary>
public class Match
{
    public Guid Id { get; private set; }
    public Guid PlayerOneId { get; private set; }
    public Guid PlayerTwoId { get; private set; }
    public int PlayerOneScore { get; private set; }
    public int PlayerTwoScore { get; private set; }
    public DateTime PlayedAt { get; private set; }

    /// <summary>Deduplicates recording. Client-supplied or derived from the payload.</summary>
    public Guid IdempotencyKey { get; private set; }

    // Required by EF Core materialization.
    private Match()
    {
    }

    public Match(
        Guid id,
        Guid playerOneId,
        Guid playerTwoId,
        int playerOneScore,
        int playerTwoScore,
        DateTime playedAt,
        Guid idempotencyKey)
    {
        Id = id;
        PlayerOneId = playerOneId;
        PlayerTwoId = playerTwoId;
        PlayerOneScore = playerOneScore;
        PlayerTwoScore = playerTwoScore;
        PlayedAt = playedAt;
        IdempotencyKey = idempotencyKey;
    }

    public static Match Record(
        Guid playerOneId,
        Guid playerTwoId,
        int playerOneScore,
        int playerTwoScore,
        DateTime playedAt,
        Guid idempotencyKey)
        => new(Guid.NewGuid(), playerOneId, playerTwoId, playerOneScore, playerTwoScore, playedAt, idempotencyKey);
}
