namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when a client attempts to complete an upload after its presigned URL has expired.
/// </summary>
public class UploadExpiredException : DomainException
{
    public Guid FileId { get; }
    public DateTime ExpiredAt { get; }

    public UploadExpiredException(Guid fileId, DateTime expiredAt)
        : base($"Upload for file '{fileId}' expired at {expiredAt:O}.")
    {
        FileId = fileId;
        ExpiredAt = expiredAt;
    }
}
