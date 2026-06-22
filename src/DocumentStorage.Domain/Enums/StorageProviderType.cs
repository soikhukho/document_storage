namespace DocumentStorage.Domain.Enums;

/// <summary>
/// Identifies the storage backend used to persist a file.
/// </summary>
public enum StorageProviderType
{
    S3 = 1,
    MinIO = 2,
    Local = 3
}
