namespace DocumentStorage.Application.Queries;

/// <summary>
/// Paged search with keyword filter and sorting (SDD §16).
/// </summary>
public record SearchFilesQuery(
    Guid? ProjectId,
    string? Keyword,
    Guid? UserId,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = "asc"
);
