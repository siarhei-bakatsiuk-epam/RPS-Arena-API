using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.Application.Features.Players.Delete;

public sealed class DeletePlayerHandler(IPlayerRepository players, IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePlayerCommand>
{
    public async Task Handle(DeletePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await players.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Player", request.Id);

        // Deliberate policy: players with match history cannot be deleted
        // (preserves referential integrity). The DB FK (ON DELETE RESTRICT, Step 7)
        // is the hard guard; this pre-check produces a clear 409 message.
        if (await players.HasMatchesAsync(request.Id, cancellationToken))
        {
            throw new ConflictException("Cannot delete a player who has recorded matches.");
        }

        players.Remove(player);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
