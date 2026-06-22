namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// Configuration for the AWS S3 storage provider.
/// Bind from the "S3" section of appsettings.json.
/// </summary>
public class S3StorageOptions
{
    /// <summary>
    /// AWS access key. Leave empty to use IAM role / environment variables / AWS profile.
    /// </summary>
    public string AccessKey { get; set; } = "";

    /// <summary>
    /// AWS secret key. Leave empty to use IAM role / environment variables / AWS profile.
    /// </summary>
    public string SecretKey { get; set; } = "";

    /// <summary>
    /// S3 bucket name (required).
    /// </summary>
    public string BucketName { get; set; } = "";

    /// <summary>
    /// AWS region, e.g. "us-east-1", "ap-southeast-1".
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Optional custom endpoint URL (e.g., LocalStack: http://localhost:4566).
    /// Leave empty for real AWS S3.
    /// </summary>
    public string? ServiceUrl { get; set; }
}
