using Microsoft.EntityFrameworkCore;
using RpsArena.Match.Application.Common.Abstractions;
using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.Infrastructure.Persistence.Repositories;

public sealed class MatchRepository(MatchDbContext context) : IMatchRepository
{
    public Task<MatchEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public Task<MatchEntity?> GetByIdempotencyKeyAsync(
        Guid idempotencyKey, CancellationToken cancellationToken = default) =>
        context.Matches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<(IReadOnlyList<MatchEntity> Items, int TotalCount)> GetPagedAsync(
        Guid? playerId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Matches.AsNoTracking().AsQueryable();

        if (playerId is { } pid)
        {
            query = query.Where(m => m.PlayerOneId == pid || m.PlayerTwoId == pid);
        }

        if (from is { } fromUtc)
        {
            query = query.Where(m => m.PlayedAt >= fromUtc);
        }

        if (to is { } toUtc)
        {
            query = query.Where(m => m.PlayedAt <= toUtc);
        }

        // Most recent first; id keeps the order stable across pages.
        var ordered = query.OrderByDescending(m => m.PlayedAt).ThenBy(m => m.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(MatchEntity match, CancellationToken cancellationToken = default) =>
        await context.Matches.AddAsync(match, cancellationToken);
}
