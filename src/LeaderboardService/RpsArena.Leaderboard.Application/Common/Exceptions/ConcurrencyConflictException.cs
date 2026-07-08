namespace RpsArena.Leaderboard.Application.Common.Exceptions;

/// <summary>
/// A concurrent update changed a stats row (xmin mismatch) or a first-insert PK
/// race occurred. Propagated out of the consumer so MassTransit redelivers and
/// the retry re-reads fresh state.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("A concurrent update conflict occurred; the message should be retried.")
    {
    }
}
