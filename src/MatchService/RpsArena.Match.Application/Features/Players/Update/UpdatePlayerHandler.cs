using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.Application.Features.Players.Update;

public sealed class UpdatePlayerHandler(IPlayerRepository players, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePlayerCommand, PlayerDto>
{
    public async Task<PlayerDto> Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await players.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Player", request.Id);

        if (await players.UsernameExistsAsync(request.Username, request.Id, cancellationToken))
        {
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        }

        if (await players.EmailExistsAsync(request.Email, request.Id, cancellationToken))
        {
            throw new ConflictException($"Email '{request.Email}' is already registered.");
        }

        player.Update(request.Username, request.Email);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PlayerDto.FromEntity(player);
    }
}
