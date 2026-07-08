using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class MatchTimestampTests(ArenaFixture fixture)
{
    // Regression: playedAt that is not already UTC used to throw (Npgsql rejects
    // a non-UTC DateTime written to timestamptz) and surface as a 500. The
    // handler now normalizes to UTC, preserving the instant.
    [Theory]
    [InlineData("2026-07-02T10:00:00", "2026-07-02T10:00:00Z")]       // naive -> treated as UTC
    [InlineData("2026-07-03T10:00:00+05:00", "2026-07-03T05:00:00Z")] // offset -> converted to UTC
    [InlineData("2026-07-04T10:00:00Z", "2026-07-04T10:00:00Z")]      // already UTC (unchanged)
    public async Task Record_match_normalizes_playedAt_to_utc(string input, string expectedUtc)
    {
        var client = fixture.Match.CreateClient();
        var p1 = await client.RegisterPlayerAsync("ts");
        var p2 = await client.RegisterPlayerAsync("ts");

        var response = await client.PostAsJsonAsync("/api/matches", new
        {
            playerOneId = p1,
            playerTwoId = p2,
            playerOneScore = 1,
            playerTwoScore = 0,
            playedAt = input,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var match = await response.Content.ReadFromJsonAsync<MatchResponse>();
        var expected = DateTimeOffset.Parse(expectedUtc, CultureInfo.InvariantCulture).UtcDateTime;
        match!.PlayedAt.ToUniversalTime().Should().Be(expected);
    }
}
