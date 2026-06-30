namespace DocumentStorage.Application.Queries;

/// <summary>
/// Paged list of soft-deleted files in a project's trash bin.
/// </summary>
public record GetTrashQuery(
    Guid ProjectId,
    Guid? UserId,
    string? Keyword,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = "asc"
);
