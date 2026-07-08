using FluentAssertions;

namespace RpsArena.Leaderboard.UnitTests;

/// <summary>
/// Placeholder that proves the Leaderboard unit-test project builds, references
/// the production layers, and that xUnit + FluentAssertions are wired up.
/// Replaced by real tests from Step 11 onward.
/// </summary>
public class ScaffoldSmokeTests
{
    [Fact]
    public void TestHarness_IsWired()
    {
        true.Should().BeTrue();
    }
}
