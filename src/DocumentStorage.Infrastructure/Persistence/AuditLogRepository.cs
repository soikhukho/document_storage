using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DocumentStorage.Infrastructure.Persistence;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly DocumentStorageDbContext _db;

    public AuditLogRepository(DocumentStorageDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(
        Guid? projectId,
        AuditActorType? actorType,
        string? action,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        IQueryable<AuditLog> query = _db.AuditLogs.AsNoTracking();

        if (projectId.HasValue)
            query = query.Where(x => x.ProjectId == projectId.Value);

        if (actorType.HasValue)
            query = query.Where(x => x.ActorType == actorType.Value);

        if (!string.IsNullOrWhiteSpace(action))
        {
            var act = action.Trim();
            query = query.Where(x => x.Action.Contains(act));
        }

        if (from.HasValue)
            query = query.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Timestamp <= to.Value);

        query = query.OrderByDescending(x => x.Timestamp);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return (items, totalCount);
    }
}
