using FluentValidation;

namespace RpsArena.Match.Application.Features.Matches.Record;

public sealed class RecordMatchValidator : AbstractValidator<RecordMatchCommand>
{
    // Small tolerance so honest clock skew between client and server does not
    // reject a match recorded "now".
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(1);

    public RecordMatchValidator(TimeProvider clock)
    {
        RuleFor(x => x.PlayerOneId).NotEmpty();
        RuleFor(x => x.PlayerTwoId).NotEmpty();

        RuleFor(x => x.PlayerTwoId)
            .NotEqual(x => x.PlayerOneId)
            .WithMessage("A player cannot play against themselves.");

        RuleFor(x => x.PlayerOneScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PlayerTwoScore).GreaterThanOrEqualTo(0);
        // Draws (equal scores, including 0:0) are intentionally allowed.

        RuleFor(x => x.PlayedAt)
            .Must(playedAt =>
                playedAt.ToUniversalTime() <= clock.GetUtcNow().UtcDateTime + FutureTolerance)
            .WithMessage("playedAt cannot be in the future.");
    }
}
