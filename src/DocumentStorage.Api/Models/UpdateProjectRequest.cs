namespace DocumentStorage.Api.Models;

public record UpdateProjectRequest(
    string? Name,
    string? Description
);
