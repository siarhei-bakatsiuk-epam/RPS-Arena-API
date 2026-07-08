using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using RpsArena.Match.Application.Features.Matches.Record;

namespace RpsArena.Match.UnitTests.Features.Matches;

public class RecordMatchValidatorTests
{
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-07-08T12:00:00Z"));
    private RecordMatchValidator Sut() => new(_clock);

    private RecordMatchCommand Valid(int s1 = 3, int s2 = 1, Guid? p1 = null, Guid? p2 = null, DateTime? at = null) =>
        new(p1 ?? Guid.NewGuid(), p2 ?? Guid.NewGuid(), s1, s2,
            at ?? new DateTime(2026, 7, 8, 11, 0, 0, DateTimeKind.Utc), null);

    [Fact]
    public void Accepts_valid_match()
    {
        Sut().Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 0)] // 0:0 draw
    [InlineData(2, 2)] // 2:2 draw
    public void Allows_draws(int s1, int s2)
    {
        Sut().Validate(Valid(s1, s2)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_self_play()
    {
        var id = Guid.NewGuid();
        Sut().Validate(Valid(p1: id, p2: id)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -5)]
    public void Rejects_negative_scores(int s1, int s2)
    {
        Sut().Validate(Valid(s1, s2)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_future_playedAt()
    {
        var future = _clock.GetUtcNow().UtcDateTime.AddHours(1);
        Sut().Validate(Valid(at: future)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Allows_playedAt_within_clock_skew_tolerance()
    {
        var slightlyAhead = _clock.GetUtcNow().UtcDateTime.AddSeconds(30);
        Sut().Validate(Valid(at: slightlyAhead)).IsValid.Should().BeTrue();
    }
}
