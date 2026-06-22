using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// Shared S3-compatible logic for S3 and MinIO providers.
/// Derived classes configure the <see cref="IAmazonS3"/> client.
/// </summary>
public abstract class S3CompatibleStorageProvider : IStorageProvider
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly ILogger _logger;

    protected S3CompatibleStorageProvider(IAmazonS3 client, string bucket, ILogger logger)
    {
        _client = client;
        _bucket = bucket;
        _logger = logger;
    }

    public Task<UploadInstruction> InitUploadAsync(
        string storageKey, string contentType, long size, int expirationMinutes,
        CancellationToken ct = default)
    {
        try
        {
            var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = storageKey,
                Verb = HttpVerb.PUT,
                Expires = expires,
                ContentType = contentType
            });

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType
            };

            return Task.FromResult(new UploadInstruction(url, headers, expires));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitUpload failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to init upload for '{storageKey}'.", ex);
        }
    }

    public async Task CompleteUploadAsync(string storageKey, CancellationToken ct = default)
    {
        if (!await ExistsAsync(storageKey, ct))
            throw new StorageException($"Object '{storageKey}' not found after upload.");
    }

    public Task<string> GetDownloadUrlAsync(
        string storageKey, int expirationMinutes, CancellationToken ct = default)
    {
        try
        {
            var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = storageKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });

            return Task.FromResult(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDownloadUrl failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to generate download URL for '{storageKey}'.", ex);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await _client.DeleteObjectAsync(_bucket, storageKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to delete '{storageKey}'.", ex);
        }
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_bucket, storageKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exists check failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to check existence of '{storageKey}'.", ex);
        }
    }

    public async Task<StorageObjectMetadata?> GetMetadataAsync(
        string storageKey, CancellationToken ct = default)
    {
        try
        {
            var resp = await _client.GetObjectMetadataAsync(_bucket, storageKey, ct);
            var contentType = resp.Headers["Content-Type"] ?? "application/octet-stream";
            return new StorageObjectMetadata(
                resp.ContentLength,
                contentType,
                resp.LastModified);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMetadata failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to get metadata for '{storageKey}'.", ex);
        }
    }

    /// <summary>
    /// Creates an S3 client. Uses explicit credentials when provided,
    /// otherwise falls back to the AWS default credential chain
    /// (IAM role, environment variables, AWS profile).
    /// </summary>
    protected static IAmazonS3 CreateClient(
        string accessKey, string secretKey, AmazonS3Config config)
    {
        if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
        {
            var creds = new BasicAWSCredentials(accessKey, secretKey);
            return new AmazonS3Client(creds, config);
        }

        // Default credential chain: IAM role → env vars → AWS profile
        return new AmazonS3Client(config);
    }
}
