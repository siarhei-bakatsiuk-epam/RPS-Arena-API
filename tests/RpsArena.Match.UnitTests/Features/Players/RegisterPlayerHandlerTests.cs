using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Features.Players.Register;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.UnitTests.Features.Players;

public class RegisterPlayerHandlerTests
{
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = new(DateTimeOffset.Parse("2026-07-08T10:00:00Z"));

    private RegisterPlayerHandler CreateSut() => new(_players, _unitOfWork, _clock);

    [Fact]
    public async Task Registers_player_and_persists()
    {
        _players.UsernameExistsAsync("neo", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _players.EmailExistsAsync("neo@matrix.io", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        Player? added = null;
        await _players.AddAsync(Arg.Do<Player>(p => added = p), Arg.Any<CancellationToken>());

        var result = await CreateSut().Handle(new RegisterPlayerCommand("neo", "neo@matrix.io"), CancellationToken.None);

        result.Username.Should().Be("neo");
        result.Email.Should().Be("neo@matrix.io");
        result.CreatedAt.Should().Be(new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc));
        added.Should().NotBeNull();
        added!.Id.Should().Be(result.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_conflict_when_username_taken()
    {
        _players.UsernameExistsAsync("neo", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateSut().Handle(new RegisterPlayerCommand("neo", "neo@matrix.io"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Username*");
        await _players.DidNotReceive().AddAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_conflict_when_email_taken()
    {
        _players.UsernameExistsAsync("neo", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);
        _players.EmailExistsAsync("neo@matrix.io", Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => CreateSut().Handle(new RegisterPlayerCommand("neo", "neo@matrix.io"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Email*");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
