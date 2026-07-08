using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.Application.Features.Players.Register;

public sealed class RegisterPlayerHandler(
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
    : IRequestHandler<RegisterPlayerCommand, PlayerDto>
{
    public async Task<PlayerDto> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
        if (await players.UsernameExistsAsync(request.Username, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        }

        if (await players.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Email '{request.Email}' is already registered.");
        }

        var player = Player.Register(request.Username, request.Email, clock.GetUtcNow().UtcDateTime);

        await players.AddAsync(player, cancellationToken);
        // The unique indexes are the real guard; SaveChanges translates a race
        // (23505) into ConflictException.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return PlayerDto.FromEntity(player);
    }
}
