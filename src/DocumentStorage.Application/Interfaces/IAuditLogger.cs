using DocumentStorage.Application.DTOs;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// High-level audit logging service — persists entries independently
/// of the request's DbContext transaction scope.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry, CancellationToken ct = default);
}
