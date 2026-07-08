using FluentValidation;

namespace RpsArena.Leaderboard.Application.Features.ApplyMatchResult;

public sealed class ApplyMatchResultValidator : AbstractValidator<ApplyMatchResultCommand>
{
    public ApplyMatchResultValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.PlayerOneId).NotEmpty();
        RuleFor(x => x.PlayerTwoId).NotEmpty();
        RuleFor(x => x.PlayerOneUsername).NotEmpty();
        RuleFor(x => x.PlayerTwoUsername).NotEmpty();
        RuleFor(x => x.PlayerOneScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PlayerTwoScore).GreaterThanOrEqualTo(0);
    }
}
