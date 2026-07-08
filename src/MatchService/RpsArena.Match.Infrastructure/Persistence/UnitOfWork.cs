using Microsoft.EntityFrameworkCore;
using Npgsql;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.Infrastructure.Persistence;

/// <summary>
/// Commits the DbContext and turns PostgreSQL unique-constraint violations
/// (SQLSTATE 23505) into <see cref="ConflictException"/> so concurrent
/// duplicate writes surface as 409 rather than 500.
/// </summary>
public sealed class UnitOfWork(MatchDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        // 23505 = unique_violation, 23503 = foreign_key_violation (e.g. deleting a
        // player still referenced by a match under ON DELETE RESTRICT).
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" or "23503" } pg)
        {
            throw new ConflictException(DescribeConflict(pg));
        }
    }

    private static string DescribeConflict(PostgresException pg) => pg switch
    {
        { SqlState: "23503" } => "The player cannot be deleted because related matches exist.",
        { ConstraintName: "ix_players_username" } => "Username is already taken.",
        { ConstraintName: "ix_players_email" } => "Email is already registered.",
        { ConstraintName: "ix_matches_idempotency_key" } =>
            "A match with this idempotency key already exists.",
        _ => "The request conflicts with existing data.",
    };
}
