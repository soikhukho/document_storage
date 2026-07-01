namespace DocumentStorage.Domain.Enums;

/// <summary>
/// Identifies the type of caller that triggered an audit entry.
/// </summary>
public enum AuditActorType
{
    Admin = 0,
    Project = 1,
    Anonymous = 2
}
