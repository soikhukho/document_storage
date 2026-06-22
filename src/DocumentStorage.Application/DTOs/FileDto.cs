namespace DocumentStorage.Application.DTOs;

/// <summary>
/// File metadata returned to the client (SDD §11).
/// </summary>
public record FileDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string Extension,
    string ContentType,
    long Size,
    string DownloadUrl,
    DateTime CreatedAt,
    string Description
);
