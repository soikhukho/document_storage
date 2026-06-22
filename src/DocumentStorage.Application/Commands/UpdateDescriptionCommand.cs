namespace DocumentStorage.Application.Commands;

public record UpdateDescriptionCommand(
    Guid ProjectId,
    Guid FileId,
    Guid? UserId,
    string Description
);
