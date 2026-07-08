using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.Application.Features.Players;

public sealed record PlayerDto(Guid Id, string Username, string Email, DateTime CreatedAt)
{
    public static PlayerDto FromEntity(Player player) =>
        new(player.Id, player.Username, player.Email, player.CreatedAt);
}
