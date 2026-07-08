using FluentValidation;
using MediatR;
using RpsArena.Leaderboard.Application.Common.Abstractions;

namespace RpsArena.Leaderboard.Application.Features.Leaderboard.GetLeaderboard;

public sealed record GetLeaderboardQuery(string? SortBy = null, int Top = 10)
    : IRequest<IReadOnlyList<PlayerStatsDto>>;

public sealed class GetLeaderboardValidator : AbstractValidator<GetLeaderboardQuery>
{
    public GetLeaderboardValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy)
            .Must(v => LeaderboardSortByParser.TryParse(v, out _))
            .WithMessage("sortBy must be one of: wins, draws, losses, matchPoints, totalScore.");
    }
}

public sealed class GetLeaderboardHandler(IPlayerStatsRepository stats)
    : IRequestHandler<GetLeaderboardQuery, IReadOnlyList<PlayerStatsDto>>
{
    public async Task<IReadOnlyList<PlayerStatsDto>> Handle(
        GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        LeaderboardSortByParser.TryParse(request.SortBy, out var sortBy);
        return await stats.GetLeaderboardAsync(sortBy, request.Top, cancellationToken);
    }
}
