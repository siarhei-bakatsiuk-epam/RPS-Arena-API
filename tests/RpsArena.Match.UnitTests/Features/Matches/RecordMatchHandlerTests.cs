using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RpsArena.Contracts;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Features.Matches.Record;
using RpsArena.Match.Domain.Entities;
using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.UnitTests.Features.Matches;

public class RecordMatchHandlerTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _events = Substitute.For<IEventPublisher>();

    private static readonly Guid P1 = Guid.NewGuid();
    private static readonly Guid P2 = Guid.NewGuid();
    private static readonly DateTime Played = new(2026, 7, 8, 11, 0, 0, DateTimeKind.Utc);

    private RecordMatchHandler Sut() => new(_matches, _players, _unitOfWork, _events);

    private void BothPlayersExist()
    {
        _players.GetByIdAsync(P1, Arg.Any<CancellationToken>())
            .Returns(new Player(P1, "alice", "alice@x.io", DateTime.UtcNow));
        _players.GetByIdAsync(P2, Arg.Any<CancellationToken>())
            .Returns(new Player(P2, "bob", "bob@x.io", DateTime.UtcNow));
    }

    private RecordMatchCommand Command(int s1 = 3, int s2 = 1, Guid? key = null) =>
        new(P1, P2, s1, s2, Played, key);

    [Fact]
    public async Task Records_new_match()
    {
        BothPlayersExist();
        var key = Guid.NewGuid();
        _matches.GetByIdempotencyKeyAsync(key, Arg.Any<CancellationToken>()).Returns((MatchEntity?)null);

        MatchEntity? added = null;
        await _matches.AddAsync(Arg.Do<MatchEntity>(m => added = m), Arg.Any<CancellationToken>());

        var result = await Sut().Handle(Command(key: key), CancellationToken.None);

        result.AlreadyExisted.Should().BeFalse();
        result.Match.PlayerOneScore.Should().Be(3);
        added.Should().NotBeNull();
        added!.IdempotencyKey.Should().Be(key);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        // Event enrolled with denormalized usernames from the fetched players.
        await _events.Received(1).PublishAsync(
            Arg.Is<MatchRecorded>(e =>
                e.MatchId == added.Id &&
                e.PlayerOneUsername == "alice" &&
                e.PlayerTwoUsername == "bob" &&
                e.PlayerOneScore == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Records_0_0_draw()
    {
        BothPlayersExist();
        _matches.GetByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MatchEntity?)null);

        var result = await Sut().Handle(Command(0, 0), CancellationToken.None);

        result.AlreadyExisted.Should().BeFalse();
        result.Match.PlayerOneScore.Should().Be(0);
        result.Match.PlayerTwoScore.Should().Be(0);
    }

    [Fact]
    public async Task Replays_existing_match_with_matching_payload()
    {
        var key = Guid.NewGuid();
        var existing = new MatchEntity(Guid.NewGuid(), P1, P2, 3, 1, Played, key);
        _matches.GetByIdempotencyKeyAsync(key, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Sut().Handle(Command(key: key), CancellationToken.None);

        result.AlreadyExisted.Should().BeTrue();
        result.Match.Id.Should().Be(existing.Id);
        await _matches.DidNotReceive().AddAsync(Arg.Any<MatchEntity>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        // No ghost event on replay.
        await _events.DidNotReceive().PublishAsync(Arg.Any<MatchRecorded>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Conflicts_when_key_reused_with_different_payload()
    {
        var key = Guid.NewGuid();
        var existing = new MatchEntity(Guid.NewGuid(), P1, P2, 3, 1, Played, key);
        _matches.GetByIdempotencyKeyAsync(key, Arg.Any<CancellationToken>()).Returns(existing);

        // Same key, different scores.
        var act = () => Sut().Handle(Command(5, 0, key), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task NotFound_when_a_player_is_missing()
    {
        _matches.GetByIdempotencyKeyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MatchEntity?)null);
        _players.GetByIdAsync(P1, Arg.Any<CancellationToken>())
            .Returns(new Player(P1, "alice", "alice@x.io", DateTime.UtcNow));
        _players.GetByIdAsync(P2, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var act = () => Sut().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _events.DidNotReceive().PublishAsync(Arg.Any<MatchRecorded>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Insert_race_falls_back_to_replay()
    {
        BothPlayersExist();
        var key = Guid.NewGuid();
        var raced = new MatchEntity(Guid.NewGuid(), P1, P2, 3, 1, Played, key);

        // First lookup: not found. SaveChanges loses the race -> Conflict.
        // Second lookup (after the race): the winner's row.
        _matches.GetByIdempotencyKeyAsync(key, Arg.Any<CancellationToken>())
            .Returns((MatchEntity?)null, raced);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Throws(new ConflictException("duplicate idempotency key"));

        var result = await Sut().Handle(Command(key: key), CancellationToken.None);

        result.AlreadyExisted.Should().BeTrue();
        result.Match.Id.Should().Be(raced.Id);
    }

    [Fact]
    public async Task Derived_key_is_deterministic_for_identical_payloads()
    {
        BothPlayersExist();
        var seen = new List<Guid>();
        _matches.GetByIdempotencyKeyAsync(Arg.Do<Guid>(seen.Add), Arg.Any<CancellationToken>())
            .Returns((MatchEntity?)null);

        await Sut().Handle(Command(), CancellationToken.None);   // no key -> derived
        await Sut().Handle(Command(), CancellationToken.None);   // identical -> same derived key

        seen.Should().HaveCount(2);
        seen[0].Should().Be(seen[1]);
        seen[0].Should().NotBe(Guid.Empty);
    }
}
