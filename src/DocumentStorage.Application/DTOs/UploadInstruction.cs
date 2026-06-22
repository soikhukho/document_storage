namespace DocumentStorage.Application.DTOs;

/// <summary>
/// Response from a storage provider when generating a presigned upload URL (SDD §10).
/// </summary>
public record UploadInstruction(
    string UploadUrl,
    IReadOnlyDictionary<string, string> Headers,
    DateTime ExpiredAt
);
