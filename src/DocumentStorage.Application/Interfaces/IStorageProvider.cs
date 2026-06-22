using DocumentStorage.Application.DTOs;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Abstracts storage operations so the business layer is provider-agnostic.
/// Implemented by S3StorageProvider, MinioStorageProvider, LocalStorageProvider.
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Generates a presigned upload URL and required headers for direct client upload.
    /// </summary>
    Task<UploadInstruction> InitUploadAsync(
        string storageKey,
        string contentType,
        long size,
        int expirationMinutes,
        CancellationToken ct = default);

    /// <summary>
    /// Confirms that an upload completed successfully in storage.
    /// </summary>
    Task CompleteUploadAsync(string storageKey, CancellationToken ct = default);

    /// <summary>
    /// Generates a short-lived presigned download URL.
    /// </summary>
    Task<string> GetDownloadUrlAsync(
        string storageKey,
        int expirationMinutes,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes the object from storage.
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the object exists in storage.
    /// </summary>
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);

    /// <summary>
    /// Retrieves object metadata directly from storage.
    /// </summary>
    Task<StorageObjectMetadata?> GetMetadataAsync(string storageKey, CancellationToken ct = default);
}
