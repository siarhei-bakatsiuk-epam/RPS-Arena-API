namespace RpsArena.Match.Application.Common.Models;

/// <summary>Standard envelope for paged list responses.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
