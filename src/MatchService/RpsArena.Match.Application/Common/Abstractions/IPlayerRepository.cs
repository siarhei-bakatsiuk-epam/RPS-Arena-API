using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.Application.Common.Abstractions;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(
        string username, Guid? excludingPlayerId = null, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email, Guid? excludingPlayerId = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Player> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>True if the player appears in any recorded match (delete guard).</summary>
    Task<bool> HasMatchesAsync(Guid playerId, CancellationToken cancellationToken = default);

    Task AddAsync(Player player, CancellationToken cancellationToken = default);

    void Remove(Player player);
}
