namespace DocumentStorage.Api.Models;

public record InitUploadRequest(
    string Name,
    string ContentType,
    long Size
);
