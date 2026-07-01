using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Data access abstraction for querying <see cref="AuditLog"/> entries.
/// </summary>
public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        Guid? projectId,
        AuditActorType? actorType,
        string? action,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
