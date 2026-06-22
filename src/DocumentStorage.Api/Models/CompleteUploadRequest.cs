namespace DocumentStorage.Api.Models;

public record CompleteUploadRequest(
    Guid FileId,
    string Name,
    string ContentType,
    long Size,
    string? Description = null
);
