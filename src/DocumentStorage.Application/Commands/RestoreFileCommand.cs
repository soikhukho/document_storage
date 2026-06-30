namespace DocumentStorage.Application.Commands;

public record RestoreFileCommand(
    Guid ProjectId,
    Guid FileId,
    Guid? UserId = null
);
