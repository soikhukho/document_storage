using DocumentStorage.Domain.Enums;

namespace DocumentStorage.Application.DTOs;

/// <summary>
/// Request DTO for creating an audit log entry.
/// </summary>
public sealed record AuditLogEntry(
    string HttpMethod,
    string Path,
    string Action,
    int StatusCode,
    AuditActorType ActorType,
    string? ActorId,
    Guid? ProjectId,
    string? EntityId,
    string? IPAddress,
    string? UserAgent,
    string? Details = null);
