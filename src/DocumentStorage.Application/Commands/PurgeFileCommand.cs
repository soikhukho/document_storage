namespace DocumentStorage.Application.Commands;

/// <summary>
/// Permanently removes a soft-deleted file: deletes the S3 object and the DB row.
/// </summary>
public record PurgeFileCommand(
    Guid ProjectId,
    Guid FileId,
    Guid? UserId = null
);
