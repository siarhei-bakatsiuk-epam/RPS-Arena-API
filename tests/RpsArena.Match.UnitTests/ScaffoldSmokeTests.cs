using FluentAssertions;

namespace RpsArena.Match.UnitTests;

/// <summary>
/// Placeholder that proves the Match unit-test project builds, references the
/// production layers, and that xUnit + FluentAssertions are wired up. Replaced
/// by real tests from Step 5 onward.
/// </summary>
public class ScaffoldSmokeTests
{
    [Fact]
    public void TestHarness_IsWired()
    {
        true.Should().BeTrue();
    }
}
