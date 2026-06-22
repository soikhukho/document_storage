namespace DocumentStorage.Application.Queries;

/// <summary>
/// Lists files belonging to a specific user (SDD §14).
/// </summary>
public record GetFilesByUserQuery(
    Guid? ProjectId,
    Guid UserId,
    int Page = 1,
    int PageSize = 20
);
