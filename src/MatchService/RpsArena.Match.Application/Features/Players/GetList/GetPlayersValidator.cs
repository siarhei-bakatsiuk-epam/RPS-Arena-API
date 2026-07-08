using FluentValidation;

namespace RpsArena.Match.Application.Features.Players.GetList;

public sealed class GetPlayersValidator : AbstractValidator<GetPlayersQuery>
{
    public GetPlayersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
