namespace RpsArena.Match.Application.Common.Abstractions;

/// <summary>
/// Commits the current unit of work. Implementation translates unique-constraint
/// violations (idempotency-key / username / email races) into ConflictException.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
