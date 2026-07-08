using System.Net.Http.Json;

namespace RpsArena.IntegrationTests;

/// <summary>Small helpers to keep the HTTP-flow tests readable.</summary>
public static class ApiClientExtensions
{
    public static string UniqueUsername(string prefix) =>
        $"{prefix}_{Guid.NewGuid():N}"[..Math.Min(32, prefix.Length + 1 + 32)];

    public static async Task<Guid> RegisterPlayerAsync(this HttpClient client, string prefix = "p")
    {
        var username = UniqueUsername(prefix);
        var response = await client.PostAsJsonAsync(
            "/api/players", new { username, email = $"{username}@x.io" });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();
        return player!.Id;
    }
}

public sealed record PlayerResponse(Guid Id, string Username, string Email, DateTime CreatedAt);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record MatchResponse(
    Guid Id, Guid PlayerOneId, Guid PlayerTwoId,
    int PlayerOneScore, int PlayerTwoScore, DateTime PlayedAt, Guid IdempotencyKey);

public sealed record PlayerStatsResponse(
    int Rank, Guid PlayerId, string Username,
    int Wins, int Losses, int Draws, int TotalMatches, int MatchPoints, int TotalScore);
