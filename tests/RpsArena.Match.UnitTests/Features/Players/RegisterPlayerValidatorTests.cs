using FluentAssertions;
using RpsArena.Match.Application.Features.Players.Register;

namespace RpsArena.Match.UnitTests.Features.Players;

public class RegisterPlayerValidatorTests
{
    private readonly RegisterPlayerValidator _validator = new();

    [Theory]
    [InlineData("abc", "a@b.com")]
    [InlineData("Player_1", "player.one@example.com")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "x@y.co")] // 32 chars
    public void Accepts_valid_input(string username, string email)
    {
        _validator.Validate(new RegisterPlayerCommand(username, email)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]              // empty
    [InlineData("ab")]           // too short
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 33 chars, too long
    [InlineData("bad name")]     // space not allowed
    [InlineData("bad-name")]     // dash not allowed
    public void Rejects_invalid_username(string username)
    {
        var result = _validator.Validate(new RegisterPlayerCommand(username, "a@b.com"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterPlayerCommand.Username));
    }

    [Theory]
    [InlineData("")]              // empty
    [InlineData("plainaddress")] // no @
    [InlineData("foo@")]         // nothing after @
    [InlineData("@example.com")] // nothing before @
    public void Rejects_invalid_email(string email)
    {
        var result = _validator.Validate(new RegisterPlayerCommand("valid_user", email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterPlayerCommand.Email));
    }
}
