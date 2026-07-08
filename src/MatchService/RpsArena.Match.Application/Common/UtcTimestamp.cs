namespace RpsArena.Match.Application.Common;

/// <summary>
/// Single source of truth for how the API interprets incoming timestamps. All
/// timestamps are stored, compared, and filtered in UTC (PostgreSQL
/// <c>timestamptz</c> requires it). A naive, unspecified-kind value is taken to
/// already be UTC — the API's canonical zone — while local/offset values are
/// converted to the same instant in UTC. Used by the record handler, the query
/// handler, and the record validator so every path agrees.
/// </summary>
public static class UtcTimestamp
{
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value.ToUniversalTime(),
    };

    public static DateTime? Normalize(DateTime? value) =>
        value is null ? null : Normalize(value.Value);
}
