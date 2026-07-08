using FluentAssertions;
using NSubstitute;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Features.Players.Update;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.UnitTests.Features.Players;

public class UpdatePlayerHandlerTests
{
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdatePlayerHandler CreateSut() => new(_players, _unitOfWork);

    private Player ExistingPlayer(Guid id) => new(id, "old", "old@x.io", DateTime.UtcNow);

    [Fact]
    public async Task Updates_profile_and_saves()
    {
        var id = Guid.NewGuid();
        var player = ExistingPlayer(id);
        _players.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(player);
        _players.UsernameExistsAsync("new_name", id, Arg.Any<CancellationToken>()).Returns(false);
        _players.EmailExistsAsync("new@x.io", id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateSut().Handle(new UpdatePlayerCommand(id, "new_name", "new@x.io"), CancellationToken.None);

        result.Username.Should().Be("new_name");
        result.Email.Should().Be("new@x.io");
        player.Username.Should().Be("new_name");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_not_found_when_missing()
    {
        _players.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Player?)null);

        var act = () => CreateSut().Handle(new UpdatePlayerCommand(Guid.NewGuid(), "n", "n@x.io"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Excludes_self_when_checking_uniqueness()
    {
        var id = Guid.NewGuid();
        _players.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(ExistingPlayer(id));
        _players.UsernameExistsAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);
        _players.EmailExistsAsync(Arg.Any<string>(), id, Arg.Any<CancellationToken>()).Returns(false);

        await CreateSut().Handle(new UpdatePlayerCommand(id, "same", "same@x.io"), CancellationToken.None);

        // uniqueness checks must pass the id so the player's own row is ignored
        await _players.Received(1).UsernameExistsAsync("same", id, Arg.Any<CancellationToken>());
        await _players.Received(1).EmailExistsAsync("same@x.io", id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_conflict_when_new_username_taken_by_another()
    {
        var id = Guid.NewGuid();
        _players.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(ExistingPlayer(id));
        _players.UsernameExistsAsync("taken", id, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateSut().Handle(new UpdatePlayerCommand(id, "taken", "e@x.io"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Username*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
