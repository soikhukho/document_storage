namespace DocumentStorage.Application.Commands;

public record DeleteFileCommand(
    Guid ProjectId,
    Guid FileId,
    Guid UserId
);
