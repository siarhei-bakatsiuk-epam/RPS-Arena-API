using FluentAssertions;
using NSubstitute;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Features.Players.GetById;
using RpsArena.Match.Application.Features.Players.GetList;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.UnitTests.Features.Players;

public class GetPlayerQueryHandlerTests
{
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();

    [Fact]
    public async Task GetById_returns_dto_when_found()
    {
        var player = new Player(Guid.NewGuid(), "trinity", "trinity@matrix.io", DateTime.UtcNow);
        _players.GetByIdAsync(player.Id, Arg.Any<CancellationToken>()).Returns(player);

        var result = await new GetPlayerByIdHandler(_players)
            .Handle(new GetPlayerByIdQuery(player.Id), CancellationToken.None);

        result.Id.Should().Be(player.Id);
        result.Username.Should().Be("trinity");
    }

    [Fact]
    public async Task GetById_throws_not_found_when_missing()
    {
        _players.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Player?)null);

        var act = () => new GetPlayerByIdHandler(_players)
            .Handle(new GetPlayerByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetList_maps_paged_result()
    {
        var items = new[]
        {
            new Player(Guid.NewGuid(), "a", "a@x.io", DateTime.UtcNow),
            new Player(Guid.NewGuid(), "b", "b@x.io", DateTime.UtcNow),
        };
        _players.GetPagedAsync(2, 10, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Player>)items, 42));

        var result = await new GetPlayersHandler(_players)
            .Handle(new GetPlayersQuery(2, 10), CancellationToken.None);

        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(42);
        result.Items.Should().HaveCount(2);
    }
}
