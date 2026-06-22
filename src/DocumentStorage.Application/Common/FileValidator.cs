using DocumentStorage.Application.DTOs;
using DocumentStorage.Domain.Exceptions;

namespace DocumentStorage.Application.Common;

/// <summary>
/// Validates file extension and size against configured constraints (SDD §17).
/// </summary>
public static class FileValidator
{
    /// <summary>
    /// Extracts and validates the file extension. Throws on violation.
    /// </summary>
    public static string ValidateAndExtractExtension(
        string name, string contentType, long size, StorageOptions options)
    {
        if (size > options.MaxUploadSizeBytes)
            throw new ArgumentException(
                $"File size {size} bytes exceeds the maximum of {options.MaxUploadSizeBytes} bytes.");

        var extension = Path.GetExtension(name)?.TrimStart('.').ToLowerInvariant()
            ?? string.Empty;

        if (string.IsNullOrEmpty(extension))
            throw new ArgumentException("File name must include an extension.");

        if (!options.AllowedExtensions.Contains(extension))
            throw new InvalidFileTypeException(extension, contentType);

        return extension;
    }
}
