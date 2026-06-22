namespace DocumentStorage.Application.Commands;

public record CompleteUploadCommand(
    Guid ProjectId,
    Guid FileId,
    string Name,
    string ContentType,
    long Size,
    Guid UserId,
    string? Description = null
);
