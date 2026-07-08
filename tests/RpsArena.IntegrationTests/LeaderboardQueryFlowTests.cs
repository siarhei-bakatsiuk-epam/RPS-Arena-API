using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class LeaderboardQueryFlowTests(ArenaFixture fixture)
{
    [Fact]
    public async Task Leaderboard_sorts_ranks_and_serves_player_stats()
    {
        var matchClient = fixture.Match.CreateClient();
        var leaderboardClient = fixture.Leaderboard.CreateClient(); // boots the consumer

        var alice = await matchClient.RegisterPlayerAsync("lb_alice");
        var bob = await matchClient.RegisterPlayerAsync("lb_bob");
        var carol = await matchClient.RegisterPlayerAsync("lb_carol");

        await RecordAsync(matchClient, alice, bob, 3, 1, "2026-04-01T10:00:00Z");
        await RecordAsync(matchClient, alice, carol, 2, 0, "2026-04-02T10:00:00Z");

        // Wait for the leaderboard to catch up (alice now has 2 wins). Own-stats
        // are deterministic; rank is not asserted absolutely because the
        // leaderboard_db is shared across the whole integration collection.
        var aliceStats = await PollAsync(leaderboardClient, alice, s => s.TotalMatches >= 2);
        aliceStats.Should().NotBeNull();
        aliceStats!.Wins.Should().Be(2);
        aliceStats.MatchPoints.Should().Be(6);
        aliceStats.TotalScore.Should().Be(5);
        aliceStats.Rank.Should().BeGreaterThanOrEqualTo(1);

        // Every documented sortBy value is accepted and actually orders the board
        // by that field (descending), with computed ranks.
        var sorters = new (string SortBy, Func<PlayerStatsResponse, int> Key)[]
        {
            ("matchPoints", s => s.MatchPoints),
            ("totalScore", s => s.TotalScore),
            ("wins", s => s.Wins),
            ("draws", s => s.Draws),
            ("losses", s => s.Losses),
        };

        foreach (var (sortBy, key) in sorters)
        {
            var board = await GetBoardAsync(leaderboardClient, $"/api/leaderboard?sortBy={sortBy}&top=50");
            board.Should().NotBeEmpty();
            board.Should().OnlyContain(s => s.Rank >= 1);
            board.Select(key).Should().BeInDescendingOrder(because: $"sortBy={sortBy}");
        }

        // 'top' caps the number of rows returned.
        (await GetBoardAsync(leaderboardClient, "/api/leaderboard?top=1")).Should().HaveCount(1);

        // Bad sortBy -> 400.
        (await leaderboardClient.GetAsync("/api/leaderboard?sortBy=bogus"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Unknown player's stats -> 404.
        (await leaderboardClient.GetAsync($"/api/leaderboard/players/{Guid.NewGuid()}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> RecordAsync(
        HttpClient client, Guid p1, Guid p2, int s1, int s2, string playedAt)
    {
        var response = await client.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = p1, playerTwoId = p2, playerOneScore = s1, playerTwoScore = s2, playedAt,
        });
        response.EnsureSuccessStatusCode();
        var match = await response.Content.ReadFromJsonAsync<MatchResponse>();
        return match!.Id;
    }

    private static async Task<IReadOnlyList<PlayerStatsResponse>> GetBoardAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<PlayerStatsResponse>>())!;
    }

    private static async Task<PlayerStatsResponse?> PollAsync(
        HttpClient client, Guid playerId, Func<PlayerStatsResponse, bool> until, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/leaderboard/players/{playerId}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var stats = await response.Content.ReadFromJsonAsync<PlayerStatsResponse>();
                if (stats is not null && until(stats))
                {
                    return stats;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(300));
        }

        return null;
    }
}
