using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class MatchFlowTests(ArenaFixture fixture)
{
    [Fact]
    public async Task Record_match_returns_201_then_idempotent_replay_returns_200_single_row()
    {
        var client = fixture.Match.CreateClient();
        var playerOne = await client.RegisterPlayerAsync("alice");
        var playerTwo = await client.RegisterPlayerAsync("bob");
        var key = Guid.NewGuid();

        var body = new
        {
            playerOneId = playerOne,
            playerTwoId = playerTwo,
            playerOneScore = 3,
            playerTwoScore = 1,
            playedAt = "2026-07-01T10:00:00Z",
            idempotencyKey = key,
        };

        var first = await client.PostAsJsonAsync("/api/matches", body);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var replay = await client.PostAsJsonAsync("/api/matches", body);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);

        // Exactly one row persisted for this idempotency key.
        var rows = await fixture.CountAsync(
            "match_db", $"SELECT count(*) FROM matches WHERE idempotency_key = '{key}'");
        rows.Should().Be(1);
    }

    [Fact]
    public async Task Self_play_returns_400_and_unknown_player_returns_404()
    {
        var client = fixture.Match.CreateClient();
        var player = await client.RegisterPlayerAsync("solo");

        var selfPlay = await client.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = player,
            playerTwoId = player,
            playerOneScore = 1,
            playerTwoScore = 0,
            playedAt = "2026-07-01T10:00:00Z",
        });
        selfPlay.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var unknown = await client.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = player,
            playerTwoId = Guid.NewGuid(),
            playerOneScore = 1,
            playerTwoScore = 0,
            playedAt = "2026-07-01T10:00:00Z",
        });
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
