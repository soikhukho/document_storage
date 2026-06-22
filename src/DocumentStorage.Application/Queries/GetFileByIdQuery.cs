namespace DocumentStorage.Application.Queries;

/// <summary>
/// Fetches a single file by id, scoped to the requesting user (SDD §14).
/// </summary>
public record GetFileByIdQuery(
    Guid ProjectId,
    Guid FileId,
    Guid UserId
);
