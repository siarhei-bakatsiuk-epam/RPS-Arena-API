using FluentAssertions;
using FluentValidation;
using MediatR;
using RpsArena.Match.Application.Common.Behaviors;

namespace RpsArena.Match.UnitTests.Common;

public class ValidationBehaviorTests
{
    public sealed record Ping(string Name) : IRequest<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private static RequestHandlerDelegate<string> Next(string value) => () => Task.FromResult(value);

    [Fact]
    public async Task Throws_ValidationException_when_request_is_invalid()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        var act = () => behavior.Handle(new Ping(""), Next("handler-ran"), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainSingle(e => e.PropertyName == nameof(Ping.Name));
    }

    [Fact]
    public async Task Invokes_handler_when_request_is_valid()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);

        var result = await behavior.Handle(new Ping("ok"), Next("handler-ran"), CancellationToken.None);

        result.Should().Be("handler-ran");
    }

    [Fact]
    public async Task Invokes_handler_when_no_validators_registered()
    {
        var behavior = new ValidationBehavior<Ping, string>([]);

        var result = await behavior.Handle(new Ping(""), Next("handler-ran"), CancellationToken.None);

        result.Should().Be("handler-ran");
    }
}
