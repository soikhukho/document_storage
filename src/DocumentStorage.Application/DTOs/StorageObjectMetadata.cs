namespace DocumentStorage.Application.DTOs;

/// <summary>
/// Object metadata retrieved directly from the storage provider.
/// </summary>
public record StorageObjectMetadata(
    long Size,
    string ContentType,
    DateTime LastModified
);
