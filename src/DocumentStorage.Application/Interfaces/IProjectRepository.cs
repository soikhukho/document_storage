using DocumentStorage.Domain.Entities;

namespace DocumentStorage.Application.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task<(IReadOnlyList<Project> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
    Task UpdateAsync(Project project, CancellationToken ct = default);
}
