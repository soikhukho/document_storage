namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when a file extension or content type is not in the allowed set.
/// </summary>
public class InvalidFileTypeException : DomainException
{
    public string? Extension { get; }
    public string? ContentType { get; }

    public InvalidFileTypeException(string? extension, string? contentType)
        : base($"File type is not allowed. Extension: '{extension}', ContentType: '{contentType}'.")
    {
        Extension = extension;
        ContentType = contentType;
    }
}
