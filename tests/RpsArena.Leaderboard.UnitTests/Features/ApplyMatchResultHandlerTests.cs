using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Application.Common.Exceptions;
using RpsArena.Leaderboard.Application.Features.ApplyMatchResult;
using RpsArena.Leaderboard.Domain.Entities;

namespace RpsArena.Leaderboard.UnitTests.Features;

public class ApplyMatchResultHandlerTests
{
    private readonly IPlayerStatsRepository _stats = Substitute.For<IPlayerStatsRepository>();
    private readonly IProcessedMessageStore _processed = Substitute.For<IProcessedMessageStore>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly Guid MsgId = Guid.NewGuid();
    private static readonly Guid P1 = Guid.NewGuid();
    private static readonly Guid P2 = Guid.NewGuid();

    private ApplyMatchResultHandler Sut() => new(_stats, _processed, _unitOfWork);

    private ApplyMatchResultCommand Command(int s1 = 3, int s2 = 1) =>
        new(MsgId, P1, "alice", P2, "bob", s1, s2);

    [Fact]
    public async Task Skips_when_message_already_processed()
    {
        _processed.ExistsAsync(MsgId, Arg.Any<CancellationToken>()).Returns(true);

        await Sut().Handle(Command(), CancellationToken.None);

        _processed.DidNotReceive().Add(Arg.Any<Guid>());
        await _stats.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_stats_for_both_new_players_and_commits_once()
    {
        _processed.ExistsAsync(MsgId, Arg.Any<CancellationToken>()).Returns(false);
        _stats.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlayerStats?)null);

        var added = new List<PlayerStats>();
        await _stats.AddAsync(Arg.Do<PlayerStats>(added.Add), Arg.Any<CancellationToken>());

        await Sut().Handle(Command(3, 1), CancellationToken.None);

        _processed.Received(1).Add(MsgId);
        added.Should().HaveCount(2);

        var winner = added.Single(s => s.PlayerId == P1);
        var loser = added.Single(s => s.PlayerId == P2);
        winner.Wins.Should().Be(1);
        winner.MatchPoints.Should().Be(3);
        winner.TotalScore.Should().Be(3);
        loser.Losses.Should().Be(1);
        loser.MatchPoints.Should().Be(0);
        loser.TotalScore.Should().Be(1);

        // Both players + dedup marker committed in a single transaction.
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_existing_stats_row()
    {
        _processed.ExistsAsync(MsgId, Arg.Any<CancellationToken>()).Returns(false);
        var existing = PlayerStats.Create(P1, "alice");
        existing.ApplyResult(1, 0, "alice"); // pre-existing win
        _stats.GetByIdAsync(P1, Arg.Any<CancellationToken>()).Returns(existing);
        _stats.GetByIdAsync(P2, Arg.Any<CancellationToken>()).Returns((PlayerStats?)null);

        await Sut().Handle(Command(2, 2), CancellationToken.None); // draw

        existing.TotalMatches.Should().Be(2);
        existing.Wins.Should().Be(1);
        existing.Draws.Should().Be(1);
        existing.MatchPoints.Should().Be(4); // 3 + 1
        // Existing row is not re-added, only mutated.
        await _stats.DidNotReceive().AddAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_message_on_save_is_swallowed()
    {
        _processed.ExistsAsync(MsgId, Arg.Any<CancellationToken>()).Returns(false);
        _stats.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlayerStats?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new DuplicateMessageException());

        var act = () => Sut().Handle(Command(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Concurrency_conflict_propagates_for_retry()
    {
        _processed.ExistsAsync(MsgId, Arg.Any<CancellationToken>()).Returns(false);
        _stats.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PlayerStats?)null);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new ConcurrencyConflictException());

        var act = () => Sut().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
