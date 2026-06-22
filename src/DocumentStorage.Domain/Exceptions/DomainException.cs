namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Base type for all domain-level exceptions in the Document Storage service.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}
