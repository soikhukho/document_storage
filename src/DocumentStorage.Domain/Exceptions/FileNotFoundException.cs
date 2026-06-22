namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when a file metadata record cannot be located by its id.
/// </summary>
public class FileNotFoundException : DomainException
{
    public Guid FileId { get; }

    public FileNotFoundException(Guid fileId)
        : base($"File with id '{fileId}' was not found.")
    {
        FileId = fileId;
    }
}
