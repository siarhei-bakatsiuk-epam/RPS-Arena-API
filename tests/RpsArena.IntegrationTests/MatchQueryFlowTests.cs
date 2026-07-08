using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class MatchQueryFlowTests(ArenaFixture fixture)
{
    [Fact]
    public async Task Filters_paging_and_history_over_http()
    {
        var client = fixture.Match.CreateClient();
        var alice = await client.RegisterPlayerAsync("alice");
        var bob = await client.RegisterPlayerAsync("bob");
        var carol = await client.RegisterPlayerAsync("carol");

        var m1 = await RecordAsync(client, alice, bob, 3, 1, "2026-01-10T10:00:00Z");
        await RecordAsync(client, alice, bob, 2, 2, "2026-03-10T10:00:00Z");
        await RecordAsync(client, alice, carol, 0, 3, "2026-06-10T10:00:00Z");

        // Get by id / unknown id
        (await client.GetAsync($"/api/matches/{m1}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/matches/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Filter by participant
        (await GetPageAsync(client, $"/api/matches?playerId={alice}")).TotalCount.Should().Be(3);
        (await GetPageAsync(client, $"/api/matches?playerId={bob}")).TotalCount.Should().Be(2);
        (await GetPageAsync(client, $"/api/matches?playerId={carol}")).TotalCount.Should().Be(1);

        // Date filter: Jan..Apr excludes the June match
        (await GetPageAsync(client,
                $"/api/matches?playerId={alice}&from=2026-01-01T00:00:00Z&to=2026-04-01T00:00:00Z"))
            .TotalCount.Should().Be(2);

        // Pagination envelope
        var firstPage = await GetPageAsync(client, $"/api/matches?playerId={alice}&page=1&pageSize=1");
        firstPage.Items.Should().HaveCount(1);
        firstPage.TotalCount.Should().Be(3);

        // Invalid date range -> 400
        (await client.GetAsync("/api/matches?from=2026-06-01T00:00:00Z&to=2026-01-01T00:00:00Z"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Player match history
        (await GetPageAsync(client, $"/api/players/{alice}/matches")).TotalCount.Should().Be(3);

        // Unknown player's history -> 404
        (await client.GetAsync($"/api/players/{Guid.NewGuid()}/matches"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleting_a_player_with_matches_returns_409()
    {
        var client = fixture.Match.CreateClient();
        var keeper = await client.RegisterPlayerAsync("keeper");
        var rival = await client.RegisterPlayerAsync("rival");
        await RecordAsync(client, keeper, rival, 1, 0, "2026-02-02T10:00:00Z");

        (await client.DeleteAsync($"/api/players/{keeper}")).StatusCode
            .Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reusing_idempotency_key_with_different_payload_returns_409()
    {
        var client = fixture.Match.CreateClient();
        var p1 = await client.RegisterPlayerAsync("idem1");
        var p2 = await client.RegisterPlayerAsync("idem2");
        var key = Guid.NewGuid();

        var first = await client.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = p1, playerTwoId = p2, playerOneScore = 3, playerTwoScore = 0,
            playedAt = "2026-02-02T10:00:00Z", idempotencyKey = key,
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Same key, different scores -> conflict (not a silent replay).
        var conflicting = await client.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = p1, playerTwoId = p2, playerOneScore = 1, playerTwoScore = 1,
            playedAt = "2026-02-02T10:00:00Z", idempotencyKey = key,
        });
        conflicting.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

    private static async Task<PagedResponse<MatchResponse>> GetPageAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<MatchResponse>>())!;
    }
}
