using DocumentStorage.Domain.Enums;

namespace DocumentStorage.Domain.Entities;

/// <summary>
/// Metadata record for a single uploaded file.
/// </summary>
public class FileDocument
{
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Extension { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long Size { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public StorageProviderType Provider { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid UploadedBy { get; private set; }
    public string Description { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// EF Core constructor. Do not use directly — call <see cref="Create"/>.
    /// </summary>
    private FileDocument() { }

    /// <summary>
    /// Factory method enforcing entity invariants.
    /// </summary>
    public static FileDocument Create(
        Guid projectId,
        string name,
        string extension,
        string contentType,
        long size,
        string storageKey,
        StorageProviderType provider,
        Guid uploadedBy,
        string? description = null)
    {
        if (projectId == Guid.Empty)
            throw new ArgumentException("ProjectId is required.", nameof(projectId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("File name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("File extension is required.", nameof(extension));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));
        if (size <= 0)
            throw new ArgumentException("File size must be greater than zero.", nameof(size));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        if (!Enum.IsDefined(provider))
            throw new ArgumentOutOfRangeException(nameof(provider), "Unknown storage provider.");

        return new FileDocument
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name.Trim(),
            Extension = extension.Trim().TrimStart('.').ToLowerInvariant(),
            ContentType = contentType.Trim(),
            Size = size,
            StorageKey = storageKey.Trim(),
            Provider = provider,
            CreatedAt = DateTime.UtcNow,
            UploadedBy = uploadedBy,
            Description = description?.Trim() ?? string.Empty,
            IsDeleted = false,
            DeletedAt = null
        };
    }

    public void SoftDelete()
    {
        if (IsDeleted)
            return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim() ?? string.Empty;
    }
}
