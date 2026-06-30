namespace DocumentStorage.Application.DTOs;

public record ProjectDto(
    Guid Id,
    string Name,
    string FolderName,
    string Description,
    string ApiKey,
    bool IsActive,
    DateTime CreatedAt
);
