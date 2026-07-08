using FluentAssertions;
using NSubstitute;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Features.Players.Delete;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.UnitTests.Features.Players;

public class DeletePlayerHandlerTests
{
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeletePlayerHandler CreateSut() => new(_players, _unitOfWork);

    [Fact]
    public async Task Deletes_when_no_matches()
    {
        var id = Guid.NewGuid();
        var player = new Player(id, "gone", "gone@x.io", DateTime.UtcNow);
        _players.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(player);
        _players.HasMatchesAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateSut().Handle(new DeletePlayerCommand(id), CancellationToken.None);

        _players.Received(1).Remove(player);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_not_found_when_missing()
    {
        _players.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Player?)null);

        var act = () => CreateSut().Handle(new DeletePlayerCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _players.DidNotReceive().Remove(Arg.Any<Player>());
    }

    [Fact]
    public async Task Throws_conflict_when_player_has_matches()
    {
        var id = Guid.NewGuid();
        _players.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new Player(id, "busy", "busy@x.io", DateTime.UtcNow));
        _players.HasMatchesAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateSut().Handle(new DeletePlayerCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*matches*");
        _players.DidNotReceive().Remove(Arg.Any<Player>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
