namespace RpsArena.Leaderboard.Application.Common.Exceptions;

/// <summary>
/// The message was already processed (processed_messages PK violation from a
/// racing redelivery). The handler treats this as a no-op success — the stats
/// for this message were already applied by the winning delivery.
/// </summary>
public sealed class DuplicateMessageException : Exception
{
    public DuplicateMessageException() : base("Message already processed.")
    {
    }
}
