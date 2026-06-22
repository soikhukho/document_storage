using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentStorage.Infrastructure.Persistence;

public class FileDocumentRepository : IFileDocumentRepository
{
    private readonly DocumentStorageDbContext _db;

    public FileDocumentRepository(DocumentStorageDbContext db)
    {
        _db = db;
    }

    public Task<FileDocument?> GetByIdAsync(Guid id, Guid projectId, CancellationToken ct = default)
        => _db.FileDocuments.FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId, ct);

    public Task<FileDocument?> GetByIdAndUserAsync(Guid id, Guid projectId, Guid userId, CancellationToken ct = default)
        => _db.FileDocuments
            .FirstOrDefaultAsync(x => x.Id == id && x.ProjectId == projectId && x.UploadedBy == userId, ct);

    public async Task<(IReadOnlyList<FileDocument> Items, int TotalCount)> SearchAsync(
        Guid? projectId,
        string? keyword,
        Guid? userId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default)
    {
        IQueryable<FileDocument> query = _db.FileDocuments.AsNoTracking();

        if (projectId.HasValue)
            query = query.Where(x => x.ProjectId == projectId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UploadedBy == userId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(x =>
                x.Name.Contains(kw) || x.Description.Contains(kw));
        }

        query = ApplySorting(query, sortBy, sortDirection);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        return (items, totalCount);
    }

    public async Task AddAsync(FileDocument document, CancellationToken ct = default)
        => await _db.FileDocuments.AddAsync(document, ct).ConfigureAwait(false);

    public Task UpdateAsync(FileDocument document, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _db.FileDocuments.Update(document);
        return Task.CompletedTask;
    }

    private static IQueryable<FileDocument> ApplySorting(
        IQueryable<FileDocument> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant()) switch
        {
            "name"      => descending ? query.OrderByDescending(x => x.Name)      : query.OrderBy(x => x.Name),
            "size"      => descending ? query.OrderByDescending(x => x.Size)      : query.OrderBy(x => x.Size),
            "createdat" => descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _           => query.OrderByDescending(x => x.CreatedAt)
        };
    }
}
