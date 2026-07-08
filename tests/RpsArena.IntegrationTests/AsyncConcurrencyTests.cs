using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using RpsArena.Contracts;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class AsyncConcurrencyTests(ArenaFixture fixture)
{
    private const string PlayedAt = "2026-07-01T10:00:00Z";
    private static readonly DateTime PlayedAtUtc = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Recording_a_match_eventually_updates_the_leaderboard()
    {
        var matchClient = fixture.Match.CreateClient();
        var leaderboardClient = fixture.Leaderboard.CreateClient(); // boots the consumer

        var winner = await matchClient.RegisterPlayerAsync("win");
        var loser = await matchClient.RegisterPlayerAsync("lose");

        var record = await matchClient.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = winner,
            playerTwoId = loser,
            playerOneScore = 3,
            playerTwoScore = 1,
            playedAt = PlayedAt,
        });
        record.StatusCode.Should().Be(HttpStatusCode.Created);

        var stats = await PollPlayerStatsAsync(leaderboardClient, winner, s => s.TotalMatches >= 1);

        stats.Should().NotBeNull();
        stats!.Wins.Should().Be(1);
        stats.MatchPoints.Should().Be(3);
        stats.TotalScore.Should().Be(3);
    }

    [Fact]
    public async Task Fifty_parallel_events_for_one_player_produce_exact_stats()
    {
        var leaderboardClient = fixture.Leaderboard.CreateClient(); // boots the consumer
        var bus = fixture.Leaderboard.Services.GetRequiredService<IBus>();

        var hero = Guid.NewGuid();

        // 50 distinct matches, hero wins 1:0 each, published concurrently. Every
        // event updates hero's (and an opponent's) stats row -> xmin conflicts
        // that MassTransit retries. The final tally must be exact.
        var publishes = Enumerable.Range(0, 50).Select(_ =>
        {
            var opponent = Guid.NewGuid();
            return bus.Publish(new MatchRecorded(
                MatchId: Guid.NewGuid(),
                PlayerOneId: hero, PlayerOneUsername: "hero",
                PlayerTwoId: opponent, PlayerTwoUsername: "foe",
                PlayerOneScore: 1, PlayerTwoScore: 0,
                PlayedAt: PlayedAtUtc));
        });

        await Task.WhenAll(publishes);

        var stats = await PollPlayerStatsAsync(
            leaderboardClient, hero, s => s.TotalMatches >= 50, timeoutSeconds: 60);

        stats.Should().NotBeNull();
        stats!.TotalMatches.Should().Be(50);
        stats.Wins.Should().Be(50);
        stats.MatchPoints.Should().Be(150); // 50 * 3
        stats.TotalScore.Should().Be(50);   // 50 * 1
    }

    [Fact]
    public async Task Duplicate_message_id_is_counted_once()
    {
        var leaderboardClient = fixture.Leaderboard.CreateClient();
        var bus = fixture.Leaderboard.Services.GetRequiredService<IBus>();

        var player = Guid.NewGuid();
        var opponent = Guid.NewGuid();
        var messageId = NewId.NextGuid();
        var @event = new MatchRecorded(
            MatchId: Guid.NewGuid(),
            PlayerOneId: player, PlayerOneUsername: "once",
            PlayerTwoId: opponent, PlayerTwoUsername: "foe",
            PlayerOneScore: 2, PlayerTwoScore: 0,
            PlayedAt: PlayedAtUtc);

        // Same message id delivered twice -> dedup must count it once.
        await bus.Publish(@event, ctx => ctx.MessageId = messageId);
        await bus.Publish(@event, ctx => ctx.MessageId = messageId);

        var stats = await PollPlayerStatsAsync(leaderboardClient, player, s => s.TotalMatches >= 1);
        stats.Should().NotBeNull();

        // Give the duplicate time to be delivered + skipped, then assert once.
        await Task.Delay(TimeSpan.FromSeconds(3));
        var recheck = await GetPlayerStatsAsync(leaderboardClient, player);
        recheck!.TotalMatches.Should().Be(1);
        recheck.Wins.Should().Be(1);
        recheck.TotalScore.Should().Be(2);
    }

    private static async Task<PlayerStatsResponse?> GetPlayerStatsAsync(HttpClient client, Guid playerId)
    {
        var response = await client.GetAsync($"/api/leaderboard/players/{playerId}");
        return response.StatusCode == HttpStatusCode.OK
            ? await response.Content.ReadFromJsonAsync<PlayerStatsResponse>()
            : null;
    }

    private static async Task<PlayerStatsResponse?> PollPlayerStatsAsync(
        HttpClient client, Guid playerId, Func<PlayerStatsResponse, bool> until, int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var stats = await GetPlayerStatsAsync(client, playerId);
            if (stats is not null && until(stats))
            {
                return stats;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(300));
        }

        return null;
    }
}
