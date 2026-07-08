using MediatR;

namespace RpsArena.Match.Application.Features.Players.Delete;

public sealed record DeletePlayerCommand(Guid Id) : IRequest;
