using Microsoft.EntityFrameworkCore;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Domain.Entities;

namespace RpsArena.Match.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository(MatchDbContext context) : IPlayerRepository
{
    public Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Players.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> UsernameExistsAsync(
        string username, Guid? excludingPlayerId = null, CancellationToken cancellationToken = default) =>
        context.Players.AnyAsync(
            p => p.Username == username && (excludingPlayerId == null || p.Id != excludingPlayerId),
            cancellationToken);

    public Task<bool> EmailExistsAsync(
        string email, Guid? excludingPlayerId = null, CancellationToken cancellationToken = default) =>
        // citext column => equality comparison is case-insensitive in the DB.
        context.Players.AnyAsync(
            p => p.Email == email && (excludingPlayerId == null || p.Id != excludingPlayerId),
            cancellationToken);

    public async Task<(IReadOnlyList<Player> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Players.AsNoTracking().OrderBy(p => p.CreatedAt).ThenBy(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default) =>
        await context.Players.AddAsync(player, cancellationToken);

    public void Remove(Player player) => context.Players.Remove(player);
}
