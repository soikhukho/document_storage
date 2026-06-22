using DocumentStorage.Application.DTOs;
using DocumentStorage.Domain.Entities;

namespace DocumentStorage.Application.Common;

/// <summary>
/// Maps domain entities to API-facing DTOs.
/// </summary>
public static class FileMapper
{
    public static FileDto ToDto(FileDocument document, string downloadUrl) => new(
        document.Id,
        document.ProjectId,
        document.Name,
        document.Extension,
        document.ContentType,
        document.Size,
        downloadUrl,
        document.CreatedAt,
        document.Description
    );
}
