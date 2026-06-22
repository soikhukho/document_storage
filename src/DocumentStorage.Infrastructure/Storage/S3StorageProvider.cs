using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// AWS S3 storage provider.
/// </summary>
public class S3StorageProvider : S3CompatibleStorageProvider
{
    public S3StorageProvider(S3StorageOptions options, ILogger<S3StorageProvider> logger)
        : base(
            CreateClient(options),
            options.BucketName,
            logger)
    {
        ValidateOptions(options);
    }

    private static IAmazonS3 CreateClient(S3StorageOptions options)
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
        };

        // Support custom endpoint (e.g., LocalStack for testing)
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            config.ForcePathStyle = true;
        }

        return CreateClient(options.AccessKey, options.SecretKey, config);
    }

    private static void ValidateOptions(S3StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BucketName))
            throw new InvalidOperationException("S3:BucketName is required when using S3 storage provider.");
    }
}
