using FluentValidation;
using MediatR;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Application.Common.Exceptions;

namespace RpsArena.Leaderboard.Application.Features.Leaderboard.GetPlayerStats;

public sealed record GetPlayerStatsQuery(Guid PlayerId) : IRequest<PlayerStatsDto>;

public sealed class GetPlayerStatsValidator : AbstractValidator<GetPlayerStatsQuery>
{
    public GetPlayerStatsValidator() => RuleFor(x => x.PlayerId).NotEmpty();
}

public sealed class GetPlayerStatsHandler(IPlayerStatsRepository stats)
    : IRequestHandler<GetPlayerStatsQuery, PlayerStatsDto>
{
    public async Task<PlayerStatsDto> Handle(GetPlayerStatsQuery request, CancellationToken cancellationToken)
    {
        return await stats.GetRankedByIdAsync(request.PlayerId, cancellationToken)
            ?? throw new NotFoundException("Player stats", request.PlayerId);
    }
}
