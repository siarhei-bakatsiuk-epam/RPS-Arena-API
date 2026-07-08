using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.Application.Features.Players.GetById;

public sealed class GetPlayerByIdHandler(IPlayerRepository players)
    : IRequestHandler<GetPlayerByIdQuery, PlayerDto>
{
    public async Task<PlayerDto> Handle(GetPlayerByIdQuery request, CancellationToken cancellationToken)
    {
        var player = await players.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Player", request.Id);

        return PlayerDto.FromEntity(player);
    }
}
