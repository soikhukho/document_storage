namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Provides information about the authenticated user making the current request.
/// </summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
