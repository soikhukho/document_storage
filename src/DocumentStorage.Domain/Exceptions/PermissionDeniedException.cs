namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when a user attempts to access or modify a file they do not own.
/// </summary>
public class PermissionDeniedException : DomainException
{
    public Guid FileId { get; }
    public Guid UserId { get; }

    public PermissionDeniedException(Guid fileId, Guid userId)
        : base($"User '{userId}' does not have permission to access file '{fileId}'.")
    {
        FileId = fileId;
        UserId = userId;
    }
}
