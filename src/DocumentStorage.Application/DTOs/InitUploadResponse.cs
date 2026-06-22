namespace DocumentStorage.Application.DTOs;

/// <summary>
/// Full response for the init-upload endpoint (SDD §6 Step 4).
/// </summary>
public record InitUploadResponse(
    Guid FileId,
    string UploadUrl,
    IReadOnlyDictionary<string, string> Headers,
    DateTime ExpiredAt
);
