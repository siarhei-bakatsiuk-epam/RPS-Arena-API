using FluentValidation;

namespace RpsArena.Match.Application.Features.Players.Update;

public sealed class UpdatePlayerValidator : AbstractValidator<UpdatePlayerCommand>
{
    public UpdatePlayerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 32)
            .Matches("^[A-Za-z0-9_]+$")
            .WithMessage("Username may contain only letters, digits and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(320)
            .EmailAddress();
    }
}
