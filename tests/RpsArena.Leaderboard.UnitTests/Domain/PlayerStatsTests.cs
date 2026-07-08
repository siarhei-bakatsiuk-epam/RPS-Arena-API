using FluentAssertions;
using RpsArena.Leaderboard.Domain;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.UnitTests.Domain;

public class PlayerStatsTests
{
    [Theory]
    [InlineData(3, 1, MatchOutcome.Win)]
    [InlineData(1, 3, MatchOutcome.Loss)]
    [InlineData(2, 2, MatchOutcome.Draw)]
    [InlineData(0, 0, MatchOutcome.Draw)]  // 0:0 is a draw
    [InlineData(5, 0, MatchOutcome.Win)]
    public void Classify_maps_scores_to_outcome(int playerScore, int opponentScore, MatchOutcome expected)
    {
        PlayerStats.Classify(playerScore, opponentScore).Should().Be(expected);
    }

    [Theory]
    [InlineData(MatchOutcome.Win, 3)]
    [InlineData(MatchOutcome.Draw, 1)]
    [InlineData(MatchOutcome.Loss, 0)]
    public void PointsFor_uses_3_1_0_scheme(MatchOutcome outcome, int expected)
    {
        PlayerStats.PointsFor(outcome).Should().Be(expected);
    }

    [Fact]
    public void ApplyResult_win_updates_all_axes()
    {
        var stats = PlayerStats.Create(Guid.NewGuid(), "neo");

        stats.ApplyResult(playerScore: 3, opponentScore: 1, "neo");

        stats.Wins.Should().Be(1);
        stats.Losses.Should().Be(0);
        stats.Draws.Should().Be(0);
        stats.TotalMatches.Should().Be(1);
        stats.MatchPoints.Should().Be(3);
        stats.TotalScore.Should().Be(3);
    }

    [Fact]
    public void ApplyResult_loss_awards_zero_points_but_counts_rounds()
    {
        var stats = PlayerStats.Create(Guid.NewGuid(), "trinity");

        stats.ApplyResult(playerScore: 1, opponentScore: 3, "trinity");

        stats.Losses.Should().Be(1);
        stats.MatchPoints.Should().Be(0);
        stats.TotalScore.Should().Be(1);   // rounds still accumulate
        stats.TotalMatches.Should().Be(1);
    }

    [Fact]
    public void ApplyResult_scoreless_draw_gives_one_point_zero_score()
    {
        var stats = PlayerStats.Create(Guid.NewGuid(), "morpheus");

        stats.ApplyResult(playerScore: 0, opponentScore: 0, "morpheus");

        stats.Draws.Should().Be(1);
        stats.MatchPoints.Should().Be(1);
        stats.TotalScore.Should().Be(0);
        stats.TotalMatches.Should().Be(1);
    }

    [Fact]
    public void ApplyResult_accumulates_across_matches()
    {
        var stats = PlayerStats.Create(Guid.NewGuid(), "smith");

        stats.ApplyResult(3, 1, "smith");  // win  -> +3 pts, +3 score
        stats.ApplyResult(2, 2, "smith");  // draw -> +1 pts, +2 score
        stats.ApplyResult(0, 4, "smith");  // loss -> +0 pts, +0 score
        stats.ApplyResult(5, 4, "smith");  // win  -> +3 pts, +5 score

        stats.Wins.Should().Be(2);
        stats.Draws.Should().Be(1);
        stats.Losses.Should().Be(1);
        stats.TotalMatches.Should().Be(4);
        stats.MatchPoints.Should().Be(7);    // 3 + 1 + 0 + 3
        stats.TotalScore.Should().Be(10);    // 3 + 2 + 0 + 5
    }

    [Fact]
    public void ApplyResult_refreshes_username_from_latest_event()
    {
        var stats = PlayerStats.Create(Guid.NewGuid(), "old_name");

        stats.ApplyResult(1, 0, "new_name");

        stats.Username.Should().Be("new_name");
    }
}
