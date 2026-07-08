using FluentAssertions;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class MigrationSmokeTests(ArenaFixture fixture)
{
    [Fact]
    public async Task Match_schema_is_created_on_startup()
    {
        // Instantiating a client boots the host, which runs migrations.
        _ = fixture.Match.CreateClient();

        var tables = await fixture.GetTablesAsync("match_db");

        tables.Should().Contain(["players", "matches", "outbox_message"]);
    }

    [Fact]
    public async Task Leaderboard_schema_is_created_on_startup()
    {
        _ = fixture.Leaderboard.CreateClient();

        var tables = await fixture.GetTablesAsync("leaderboard_db");

        tables.Should().Contain(["player_stats", "processed_messages"]);
    }
}
