namespace DocumentStorage.Application.ProjectCommands;

public record CreateProjectCommand(
    string Name,
    string FolderName,
    string? Description
);
