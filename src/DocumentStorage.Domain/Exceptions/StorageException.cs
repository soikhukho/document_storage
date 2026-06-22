namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when an underlying storage provider operation fails.
/// Wraps provider-specific errors so the business layer stays provider-agnostic.
/// </summary>
public class StorageException : DomainException
{
    public StorageException(string message) : base(message) { }
    public StorageException(string message, Exception innerException) : base(message, innerException) { }
}
