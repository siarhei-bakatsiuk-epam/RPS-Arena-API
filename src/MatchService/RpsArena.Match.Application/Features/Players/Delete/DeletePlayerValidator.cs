using FluentValidation;

namespace RpsArena.Match.Application.Features.Players.Delete;

public sealed class DeletePlayerValidator : AbstractValidator<DeletePlayerCommand>
{
    public DeletePlayerValidator() => RuleFor(x => x.Id).NotEmpty();
}
