namespace RpsArena.Match.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with the current state (duplicate unique
/// value, deleting a player that still has matches, idempotency-key reuse with a
/// different payload). Mapped to HTTP 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
