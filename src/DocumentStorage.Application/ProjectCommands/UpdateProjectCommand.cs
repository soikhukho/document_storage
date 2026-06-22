namespace DocumentStorage.Application.ProjectCommands;

public record UpdateProjectCommand(
    Guid ProjectId,
    string? Name,
    string? Description
);
