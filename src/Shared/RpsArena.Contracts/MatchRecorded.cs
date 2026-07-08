namespace RpsArena.Contracts;

/// <summary>
/// Published by MatchService after a match is persisted; consumed by
/// LeaderboardService. Usernames are denormalized onto the event so the
/// consumer never reads MatchService's database. This is the only shared
/// contract between the services; it is versioned additively.
/// </summary>
public sealed record MatchRecorded(
    Guid MatchId,
    Guid PlayerOneId,
    string PlayerOneUsername,
    Guid PlayerTwoId,
    string PlayerTwoUsername,
    int PlayerOneScore,
    int PlayerTwoScore,
    DateTime PlayedAt);
