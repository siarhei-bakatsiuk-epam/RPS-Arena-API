using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Models;

namespace RpsArena.Match.Application.Features.Players.GetList;

public sealed class GetPlayersHandler(IPlayerRepository players)
    : IRequestHandler<GetPlayersQuery, PagedResult<PlayerDto>>
{
    public async Task<PagedResult<PlayerDto>> Handle(GetPlayersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await players.GetPagedAsync(request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(PlayerDto.FromEntity).ToList();

        return new PagedResult<PlayerDto>(dtos, request.Page, request.PageSize, totalCount);
    }
}
