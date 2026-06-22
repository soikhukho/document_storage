namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when a project cannot be located by its id or API key.
/// </summary>
public class ProjectNotFoundException : DomainException
{
    public ProjectNotFoundException(Guid projectId)
        : base($"Project with id '{projectId}' was not found.")
    {
    }
}
