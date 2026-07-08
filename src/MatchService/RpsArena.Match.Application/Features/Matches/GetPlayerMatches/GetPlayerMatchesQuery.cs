using FluentValidation;
using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Application.Common.Models;

namespace RpsArena.Match.Application.Features.Matches.GetPlayerMatches;

public sealed record GetPlayerMatchesQuery(Guid PlayerId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<MatchDto>>;

public sealed class GetPlayerMatchesValidator : AbstractValidator<GetPlayerMatchesQuery>
{
    public GetPlayerMatchesValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetPlayerMatchesHandler(IMatchRepository matches, IPlayerRepository players)
    : IRequestHandler<GetPlayerMatchesQuery, PagedResult<MatchDto>>
{
    public async Task<PagedResult<MatchDto>> Handle(GetPlayerMatchesQuery request, CancellationToken cancellationToken)
    {
        if (!await players.ExistsAsync(request.PlayerId, cancellationToken))
        {
            throw new NotFoundException("Player", request.PlayerId);
        }

        var (items, totalCount) = await matches.GetPagedAsync(
            request.PlayerId, null, null, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(MatchDto.FromEntity).ToList();
        return new PagedResult<MatchDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}
