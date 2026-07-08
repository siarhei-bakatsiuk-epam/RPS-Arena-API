using FluentAssertions;
using NSubstitute;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Features.Matches.GetById;
using RpsArena.Match.Application.Features.Matches.GetList;
using RpsArena.Match.Application.Features.Matches.GetPlayerMatches;
using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.UnitTests.Features.Matches;

public class MatchQueryTests
{
    private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();

    private static MatchEntity Sample() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 3, 1, DateTime.UtcNow, Guid.NewGuid());

    // ---- GetMatchById ----

    [Fact]
    public async Task GetById_returns_dto_when_found()
    {
        var match = Sample();
        _matches.GetByIdAsync(match.Id, Arg.Any<CancellationToken>()).Returns(match);

        var result = await new GetMatchByIdHandler(_matches)
            .Handle(new GetMatchByIdQuery(match.Id), CancellationToken.None);

        result.Id.Should().Be(match.Id);
    }

    [Fact]
    public async Task GetById_throws_not_found_when_missing()
    {
        _matches.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((MatchEntity?)null);

        var act = () => new GetMatchByIdHandler(_matches)
            .Handle(new GetMatchByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- GetMatches (filters + paging) ----

    [Fact]
    public async Task GetList_passes_filters_and_maps_result()
    {
        var pid = Guid.NewGuid();
        var from = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc);
        _matches.GetPagedAsync(pid, from, to, 2, 5, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<MatchEntity>)new[] { Sample() }, 11));

        var result = await new GetMatchesHandler(_matches)
            .Handle(new GetMatchesQuery(pid, from, to, 2, 5), CancellationToken.None);

        result.TotalCount.Should().Be(11);
        result.Page.Should().Be(2);
        result.Items.Should().HaveCount(1);
        await _matches.Received(1).GetPagedAsync(pid, from, to, 2, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetMatches_validator_rejects_from_after_to()
    {
        var v = new GetMatchesValidator();
        var q = new GetMatchesQuery(
            From: new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        v.Validate(q).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetMatches_validator_rejects_bad_paging(int page, int pageSize)
    {
        new GetMatchesValidator().Validate(new GetMatchesQuery(Page: page, PageSize: pageSize))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetMatches_validator_accepts_open_ended_date_range()
    {
        // only 'from' provided -> the from<=to rule must not fire
        new GetMatchesValidator()
            .Validate(new GetMatchesQuery(From: DateTime.UtcNow))
            .IsValid.Should().BeTrue();
    }

    // ---- GetPlayerMatches ----

    [Fact]
    public async Task PlayerMatches_throws_not_found_for_unknown_player()
    {
        _players.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => new GetPlayerMatchesHandler(_matches, _players)
            .Handle(new GetPlayerMatchesQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _matches.DidNotReceive().GetPagedAsync(
            Arg.Any<Guid?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlayerMatches_returns_history_for_known_player()
    {
        var pid = Guid.NewGuid();
        _players.ExistsAsync(pid, Arg.Any<CancellationToken>()).Returns(true);
        _matches.GetPagedAsync(pid, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<MatchEntity>)new[] { Sample(), Sample() }, 2));

        var result = await new GetPlayerMatchesHandler(_matches, _players)
            .Handle(new GetPlayerMatchesQuery(pid), CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }
}
