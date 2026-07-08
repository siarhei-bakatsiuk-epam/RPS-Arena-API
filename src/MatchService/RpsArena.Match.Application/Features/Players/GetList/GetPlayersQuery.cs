using MediatR;
using RpsArena.Match.Application.Common.Models;

namespace RpsArena.Match.Application.Features.Players.GetList;

public sealed record GetPlayersQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<PlayerDto>>;
