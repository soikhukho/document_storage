using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentStorage.Infrastructure.Persistence;

public class ProjectRepository : IProjectRepository
{
    private readonly DocumentStorageDbContext _db;

    public ProjectRepository(DocumentStorageDbContext db)
    {
        _db = db;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<Project?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default)
        => _db.Projects.FirstOrDefaultAsync(x => x.ApiKey == apiKey, ct);

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Project> query = _db.Projects.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Project project, CancellationToken ct = default)
        => await _db.Projects.AddAsync(project, ct);

    public Task UpdateAsync(Project project, CancellationToken ct = default)
    {
        _db.Projects.Update(project);
        return Task.CompletedTask;
    }
}
