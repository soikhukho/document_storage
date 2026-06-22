namespace DocumentStorage.Application.Common;

/// <summary>
/// Generates deterministic storage keys (SDD §6 Step 2).
/// Format: projects/{projectId}/users/{userId}/{year}/{month}/{fileId}.{extension}
/// </summary>
/// <remarks>
/// This format is a storage contract. Changing it after files exist
/// will orphan stored objects — existing keys must continue to resolve.
/// </remarks>
public static class StorageKeyGenerator
{
    public static string Generate(Guid projectId, Guid userId, Guid fileId, string extension)
    {
        var now = DateTime.UtcNow;
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return $"projects/{projectId}/users/{userId}/{now:yyyy}/{now:MM}/{fileId}.{ext}";
    }
}
