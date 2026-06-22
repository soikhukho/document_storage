using Amazon.S3;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// MinIO storage provider (S3-compatible, uses path-style addressing).
/// </summary>
public class MinioStorageProvider : S3CompatibleStorageProvider
{
    public MinioStorageProvider(MinioStorageOptions options, ILogger<MinioStorageProvider> logger)
        : base(
            CreateClient(options),
            options.BucketName,
            logger)
    {
    }

    private static IAmazonS3 CreateClient(MinioStorageOptions options)
    {
        ValidateOptions(options);

        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            AuthenticationRegion = options.Region
        };

        return CreateClient(options.AccessKey, options.SecretKey, config);
    }

    private static void ValidateOptions(MinioStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BucketName))
            throw new InvalidOperationException("MinIO:BucketName is required when using MinIO storage provider.");
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException("MinIO:Endpoint is required when using MinIO storage provider.");
    }
}
