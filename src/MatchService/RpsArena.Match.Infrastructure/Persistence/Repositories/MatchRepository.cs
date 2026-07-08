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

    public async Task AddAsync(MatchEntity match, CancellationToken cancellationToken = default) =>
        await context.Matches.AddAsync(match, cancellationToken);
}
