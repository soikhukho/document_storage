namespace DocumentStorage.Api.Models;

public record CreateProjectRequest(
    string Name,
    string FolderName,
    string? Description
);
