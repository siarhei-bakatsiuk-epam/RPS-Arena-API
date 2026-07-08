using FluentAssertions;

namespace RpsArena.IntegrationTests;

/// <summary>
/// Placeholder that proves the integration-test project builds. The real
/// Testcontainers (PostgreSQL + RabbitMQ) harness and HTTP flow tests arrive in
/// Steps 17–18.
/// </summary>
public class ScaffoldSmokeTests
{
    [Fact]
    public void TestHarness_IsWired()
    {
        true.Should().BeTrue();
    }
}
