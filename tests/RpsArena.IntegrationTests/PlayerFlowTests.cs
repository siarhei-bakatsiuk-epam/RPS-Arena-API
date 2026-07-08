using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RpsArena.IntegrationTests.Infrastructure;

namespace RpsArena.IntegrationTests;

[Collection(ArenaCollection.Name)]
public class PlayerFlowTests(ArenaFixture fixture)
{
    [Fact]
    public async Task Full_player_crud_flow_over_http()
    {
        var client = fixture.Match.CreateClient();
        var username = ApiClientExtensions.UniqueUsername("neo");

        // Register -> 201
        var register = await client.PostAsJsonAsync(
            "/api/players", new { username, email = $"{username}@x.io" });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await register.Content.ReadFromJsonAsync<PlayerResponse>();
        created!.Id.Should().NotBeEmpty();

        // Get -> 200
        (await client.GetAsync($"/api/players/{created.Id}")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // List -> 200
        (await client.GetAsync("/api/players?page=1&pageSize=10")).StatusCode
            .Should().Be(HttpStatusCode.OK);

        // Duplicate username -> 409
        var duplicate = await client.PostAsJsonAsync(
            "/api/players", new { username, email = $"other_{username}@x.io" });
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Update -> 200
        var newName = ApiClientExtensions.UniqueUsername("neo2");
        var update = await client.PutAsJsonAsync(
            $"/api/players/{created.Id}", new { username = newName, email = $"{newName}@x.io" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete -> 204, then Get -> 404
        (await client.DeleteAsync($"/api/players/{created.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/players/{created.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_registration_returns_problem_details_400()
    {
        var client = fixture.Match.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/players", new { username = "a b", email = "not-an-email" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
