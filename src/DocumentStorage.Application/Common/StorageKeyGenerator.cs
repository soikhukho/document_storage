namespace DocumentStorage.Application.Common;

/// <summary>
/// Generates storage keys for object storage.
/// Format: {projectName}/{fileName}
/// Flat structure: bucket → project folder → file (admin-friendly browsing).
/// </summary>
public static class StorageKeyGenerator
{
    public static string Generate(string projectName, string fileName)
    {
        var safeProject = SanitizeSegment(projectName);
        var safeFile = SanitizeSegment(fileName);
        return $"{safeProject}/{safeFile}";
    }

    /// <summary>
    /// Replaces path separators in a segment so it never creates unintended sub-folders.
    /// </summary>
    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "_";
        return value.Trim().Replace('/', '_').Replace('\\', '_');
    }
}
