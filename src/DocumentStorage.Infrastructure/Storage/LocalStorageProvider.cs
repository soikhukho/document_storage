using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Storage;

/// <summary>
/// Local file system storage provider (SDD §6 Step 5: Client → API → Local Disk).
/// Upload and download URLs point to API endpoints that stream from disk.
/// </summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly LocalStorageOptions _options;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(LocalStorageOptions options, ILogger<LocalStorageProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    private string GetFilePath(string storageKey)
    {
        var root = Path.GetFullPath(_options.BaseDirectory);
        var relative = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(root, relative));

        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !full.Equals(root, StringComparison.Ordinal))
            throw new StorageException($"Invalid storage key: '{storageKey}'.");

        return full;
    }

    private string GetBaseUrl() =>
        string.IsNullOrEmpty(_options.PublicBaseUrl) ? "/api/files" : _options.PublicBaseUrl;

    public Task<UploadInstruction> InitUploadAsync(
        string storageKey, string contentType, long size, int expirationMinutes,
        CancellationToken ct = default)
    {
        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var uploadUrl = $"{GetBaseUrl()}/local-upload/{storageKey}";

        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = contentType
        };

        return Task.FromResult(new UploadInstruction(uploadUrl, headers, expires));
    }

    public Task CompleteUploadAsync(string storageKey, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<string> GetDownloadUrlAsync(
        string storageKey, int expirationMinutes, CancellationToken ct = default)
        => Task.FromResult($"{GetBaseUrl()}/local-download/{storageKey}");

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            var path = GetFilePath(storageKey);
            if (File.Exists(path))
                File.Delete(path);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to delete '{storageKey}'.", ex);
        }
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            return Task.FromResult(File.Exists(GetFilePath(storageKey)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exists check failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to check existence of '{storageKey}'.", ex);
        }
    }

    public Task<StorageObjectMetadata?> GetMetadataAsync(
        string storageKey, CancellationToken ct = default)
    {
        try
        {
            var path = GetFilePath(storageKey);
            if (!File.Exists(path))
                return Task.FromResult<StorageObjectMetadata?>(null);

            var info = new FileInfo(path);
            return Task.FromResult<StorageObjectMetadata?>(
                new StorageObjectMetadata(info.Length, "application/octet-stream", info.LastWriteTimeUtc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMetadata failed for {StorageKey}", storageKey);
            throw new StorageException($"Failed to get metadata for '{storageKey}'.", ex);
        }
    }

    // ── Local-specific helpers (used by API layer for direct disk I/O) ──

    /// <summary>
    /// Writes uploaded content to the local disk.
    /// Called by the local-upload API endpoint.
    /// </summary>
    public async Task WriteAsync(string storageKey, Stream content, CancellationToken ct = default)
    {
        var path = GetFilePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);
        await content.CopyToAsync(fs, ct);
    }

    /// <summary>
    /// Opens a file stream for reading.
    /// Called by the local-download API endpoint.
    /// </summary>
    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var path = GetFilePath(storageKey);
        if (!File.Exists(path))
            throw new StorageException($"File not found: '{storageKey}'.");

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);
        return Task.FromResult(stream);
    }
}
