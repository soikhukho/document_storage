namespace DocumentStorage.Application.Queries;

/// <summary>
/// Fetches a single file by id, scoped to the requesting user (SDD §14).
/// When <see cref="UserId"/> is null, the lookup is project-scoped only (admin access).
/// </summary>
public record GetFileByIdQuery(
    Guid ProjectId,
    Guid FileId,
    Guid? UserId = null
);
