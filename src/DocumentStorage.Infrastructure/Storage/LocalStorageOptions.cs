namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// Configuration for the local file system storage provider.
/// </summary>
public class LocalStorageOptions
{
    public string BaseDirectory { get; set; } = "uploads";
    public string PublicBaseUrl { get; set; } = "";
}
