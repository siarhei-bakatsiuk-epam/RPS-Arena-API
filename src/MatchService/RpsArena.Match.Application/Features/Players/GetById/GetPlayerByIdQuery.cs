using MediatR;

namespace RpsArena.Match.Application.Features.Players.GetById;

public sealed record GetPlayerByIdQuery(Guid Id) : IRequest<PlayerDto>;
