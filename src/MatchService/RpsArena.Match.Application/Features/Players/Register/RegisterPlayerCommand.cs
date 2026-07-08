using MediatR;

namespace RpsArena.Match.Application.Features.Players.Register;

public sealed record RegisterPlayerCommand(string Username, string Email) : IRequest<PlayerDto>;
