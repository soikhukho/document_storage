namespace DocumentStorage.Application.Commands;

public record InitUploadCommand(
    Guid ProjectId,
    string Name,
    string ContentType,
    long Size,
    Guid UserId
);
