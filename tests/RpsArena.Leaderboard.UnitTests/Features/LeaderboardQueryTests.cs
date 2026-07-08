using FluentAssertions;
using NSubstitute;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Application.Common.Exceptions;
using RpsArena.Leaderboard.Application.Features.Leaderboard;
using RpsArena.Leaderboard.Application.Features.Leaderboard.GetLeaderboard;
using RpsArena.Leaderboard.Application.Features.Leaderboard.GetPlayerStats;

namespace RpsArena.Leaderboard.UnitTests.Features;

public class LeaderboardQueryTests
{
    private readonly IPlayerStatsRepository _stats = Substitute.For<IPlayerStatsRepository>();

    // ---- validators ----

    [Theory]
    [InlineData("matchPoints")]
    [InlineData("wins")]
    [InlineData("draws")]
    [InlineData("losses")]
    [InlineData("totalScore")]
    [InlineData("TOTALSCORE")] // case-insensitive
    [InlineData(null)]         // default
    public void Leaderboard_validator_accepts_valid_sortBy(string? sortBy)
    {
        new GetLeaderboardValidator().Validate(new GetLeaderboardQuery(sortBy, 10)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("points")]
    [InlineData("rank")]
    [InlineData("nonsense")]
    public void Leaderboard_validator_rejects_bad_sortBy(string sortBy)
    {
        new GetLeaderboardValidator().Validate(new GetLeaderboardQuery(sortBy, 10)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Leaderboard_validator_rejects_bad_top(int top)
    {
        new GetLeaderboardValidator().Validate(new GetLeaderboardQuery("wins", top)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PlayerStats_validator_rejects_empty_id()
    {
        new GetPlayerStatsValidator().Validate(new GetPlayerStatsQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }

    // ---- handlers ----

    [Fact]
    public async Task Leaderboard_handler_parses_sortBy_and_delegates()
    {
        _stats.GetLeaderboardAsync(LeaderboardSortBy.TotalScore, 5, Arg.Any<CancellationToken>())
            .Returns(new List<PlayerStatsDto>());

        await new GetLeaderboardHandler(_stats)
            .Handle(new GetLeaderboardQuery("totalScore", 5), CancellationToken.None);

        await _stats.Received(1).GetLeaderboardAsync(LeaderboardSortBy.TotalScore, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaderboard_handler_defaults_to_matchPoints()
    {
        _stats.GetLeaderboardAsync(LeaderboardSortBy.MatchPoints, 10, Arg.Any<CancellationToken>())
            .Returns(new List<PlayerStatsDto>());

        await new GetLeaderboardHandler(_stats)
            .Handle(new GetLeaderboardQuery(null, 10), CancellationToken.None);

        await _stats.Received(1).GetLeaderboardAsync(LeaderboardSortBy.MatchPoints, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayerStats_handler_returns_dto_when_found()
    {
        var id = Guid.NewGuid();
        var dto = new PlayerStatsDto(1, id, "alice", 1, 0, 0, 1, 3, 3);
        _stats.GetRankedByIdAsync(id, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await new GetPlayerStatsHandler(_stats)
            .Handle(new GetPlayerStatsQuery(id), CancellationToken.None);

        result.Rank.Should().Be(1);
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task PlayerStats_handler_throws_not_found_when_missing()
    {
        _stats.GetRankedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlayerStatsDto?)null);

        var act = () => new GetPlayerStatsHandler(_stats)
            .Handle(new GetPlayerStatsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
