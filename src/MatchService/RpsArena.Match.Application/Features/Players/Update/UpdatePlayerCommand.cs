using MediatR;

namespace RpsArena.Match.Application.Features.Players.Update;

public sealed record UpdatePlayerCommand(Guid Id, string Username, string Email) : IRequest<PlayerDto>;
