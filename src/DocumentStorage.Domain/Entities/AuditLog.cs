using DocumentStorage.Domain.Enums;

namespace DocumentStorage.Domain.Entities;

/// <summary>
/// Immutable audit trail entry for mutating API operations.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string HttpMethod { get; private set; } = null!;
    public string Path { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public int StatusCode { get; private set; }
    public bool Success { get; private set; }

    /// <summary>Admin, Project, or Anonymous.</summary>
    public AuditActorType ActorType { get; private set; }

    /// <summary>Admin user id, project id, or null.</summary>
    public string? ActorId { get; private set; }

    public Guid? ProjectId { get; private set; }
    public string? EntityId { get; private set; }
    public string? IPAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Details { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string httpMethod,
        string path,
        string action,
        int statusCode,
        AuditActorType actorType,
        string? actorId,
        Guid? projectId,
        string? entityId,
        string? ipAddress,
        string? userAgent,
        string? details = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            HttpMethod = httpMethod,
            Path = path,
            Action = action,
            StatusCode = statusCode,
            Success = statusCode < 400,
            ActorType = actorType,
            ActorId = actorId,
            ProjectId = projectId,
            EntityId = entityId,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            Details = details
        };
    }
}
