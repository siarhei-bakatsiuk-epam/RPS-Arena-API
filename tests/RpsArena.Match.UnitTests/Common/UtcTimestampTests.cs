using FluentAssertions;
using RpsArena.Match.Application.Common;

namespace RpsArena.Match.UnitTests.Common;

public class UtcTimestampTests
{
    [Fact]
    public void Utc_value_is_unchanged()
    {
        var utc = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);

        var result = UtcTimestamp.Normalize(utc);

        result.Should().Be(utc);
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Unspecified_value_is_treated_as_utc_wall_clock_preserved()
    {
        var naive = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Unspecified);

        var result = UtcTimestamp.Normalize(naive);

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc)); // no shift
    }

    [Fact]
    public void Local_value_is_converted_to_the_same_instant_in_utc()
    {
        var local = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Local);

        var result = UtcTimestamp.Normalize(local);

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(local.ToUniversalTime()); // instant preserved
    }

    [Fact]
    public void Nullable_overload_passes_null_through()
    {
        UtcTimestamp.Normalize((DateTime?)null).Should().BeNull();
        UtcTimestamp.Normalize((DateTime?)new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Unspecified))
            .Should().Be(new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc));
    }
}
