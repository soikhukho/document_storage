namespace DocumentStorage.Application.ProjectCommands;

public record SetProjectActiveCommand(
    Guid ProjectId,
    bool IsActive
);
