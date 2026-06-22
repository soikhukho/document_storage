namespace DocumentStorage.Application.Common;

/// <summary>
/// Generates deterministic storage keys (SDD §6 Step 2).
/// Format: projects/{projectId}/users/{userId}/{year}/{month}/{fileId}.{extension}
/// </summary>
public static class StorageKeyGenerator
{
    public static string Generate(Guid projectId, Guid userId, Guid fileId, string extension)
    {
        var now = DateTime.UtcNow;
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return $"projects/{projectId}/users/{userId}/{now:yyyy}/{now:MM}/{fileId}.{ext}";
    }
}
