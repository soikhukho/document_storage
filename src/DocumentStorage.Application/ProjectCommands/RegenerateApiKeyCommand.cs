namespace DocumentStorage.Application.ProjectCommands;

public record RegenerateApiKeyCommand(
    Guid ProjectId
);
