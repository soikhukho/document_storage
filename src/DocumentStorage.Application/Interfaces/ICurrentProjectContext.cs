using DocumentStorage.Domain.Entities;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Provides the current project context resolved from the request.
/// </summary>
public interface ICurrentProjectContext
{
    Guid ProjectId { get; }
    bool IsAvailable { get; }
}
