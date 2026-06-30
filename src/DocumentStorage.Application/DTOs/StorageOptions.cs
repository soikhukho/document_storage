using System.Text.Json.Serialization;
using DocumentStorage.Domain.Enums;

namespace DocumentStorage.Application.DTOs;

/// <summary>
/// Storage configuration bound from the "Storage" section of appsettings.json (SDD §17, §18).
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StorageProviderType Provider { get; set; } = StorageProviderType.Local;

    /// <summary>
    /// Maximum upload size in bytes (SDD §17: 100 MB).
    /// </summary>
    public long MaxUploadSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Presigned upload URL lifetime in minutes (SDD §21: 5 minutes).
    /// </summary>
    public int UploadExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Presigned download URL lifetime in minutes (SDD §21: 1 minute).
    /// </summary>
    public int DownloadExpirationMinutes { get; set; } = 1;

    /// <summary>
    /// Allowed file extensions (SDD §17).
    /// </summary>
    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf", "docx", "xlsx", "png", "jpg", "jpeg"
    };
}
