using Microsoft.EntityFrameworkCore;
using Npgsql;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Application.Common.Exceptions;

namespace RpsArena.Leaderboard.Infrastructure.Persistence;

/// <summary>
/// Commits and maps persistence races to domain signals:
/// - xmin mismatch (optimistic concurrency) -> ConcurrencyConflictException
/// - processed_messages PK violation -> DuplicateMessageException (already done)
/// - other unique/PK violation (e.g. first-insert stats race) -> ConcurrencyConflictException
/// </summary>
public sealed class UnitOfWork(LeaderboardDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" } pg)
        {
            throw pg.ConstraintName == "pk_processed_messages"
                ? new DuplicateMessageException()
                : new ConcurrencyConflictException();
        }
    }
}
