namespace RpsArena.Leaderboard.Application.Common.Abstractions;

/// <summary>
/// Commits the current unit of work. The implementation translates persistence
/// races into domain signals: xmin/PK conflicts -> ConcurrencyConflictException
/// (retry), processed_messages PK violation -> DuplicateMessageException (skip).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
