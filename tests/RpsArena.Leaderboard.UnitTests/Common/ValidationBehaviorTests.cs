using FluentAssertions;
using FluentValidation;
using MediatR;
using RpsArena.Leaderboard.Application.Common.Behaviors;

namespace RpsArena.Leaderboard.UnitTests.Common;

public class ValidationBehaviorTests
{
    public sealed record Ping(string Name) : IRequest<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private static RequestHandlerDelegate<string> Next(string value) => () => Task.FromResult(value);

    [Fact]
    public async Task Throws_when_invalid()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        var act = () => behavior.Handle(new Ping(""), Next("ran"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Invokes_handler_when_valid()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        (await behavior.Handle(new Ping("ok"), Next("ran"), CancellationToken.None)).Should().Be("ran");
    }

    [Fact]
    public async Task Invokes_handler_when_no_validators()
    {
        var behavior = new ValidationBehavior<Ping, string>([]);

        (await behavior.Handle(new Ping(""), Next("ran"), CancellationToken.None)).Should().Be("ran");
    }
}
