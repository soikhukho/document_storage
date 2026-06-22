namespace DocumentStorage.Application.DTOs;

/// <summary>
/// Generic paged result for search/list queries (SDD §16).
/// </summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = pageSize > 0
            ? (int)Math.Ceiling(totalCount / (double)pageSize)
            : 0;
        return new PagedResult<T>(items, page, pageSize, totalCount, totalPages);
    }
}
