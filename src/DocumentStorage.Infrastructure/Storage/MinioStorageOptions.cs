namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// Configuration for the MinIO storage provider (S3-compatible).
/// Bind from the "MinIO" section of appsettings.json.
/// </summary>
public class MinioStorageOptions
{
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";

    /// <summary>
    /// MinIO bucket name (required).
    /// </summary>
    public string BucketName { get; set; } = "";

    /// <summary>
    /// MinIO server endpoint, e.g. "http://localhost:9000".
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:9000";

    /// <summary>
    /// Region for SigV4 signing (MinIO typically accepts any value).
    /// </summary>
    public string Region { get; set; } = "us-east-1";
}
