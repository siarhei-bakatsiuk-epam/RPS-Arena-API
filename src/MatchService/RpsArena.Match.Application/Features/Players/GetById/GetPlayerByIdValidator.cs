using FluentValidation;

namespace RpsArena.Match.Application.Features.Players.GetById;

public sealed class GetPlayerByIdValidator : AbstractValidator<GetPlayerByIdQuery>
{
    public GetPlayerByIdValidator() => RuleFor(x => x.Id).NotEmpty();
}
