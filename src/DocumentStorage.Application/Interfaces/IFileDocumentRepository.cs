using DocumentStorage.Domain.Entities;

namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Data access abstraction for <see cref="FileDocument"/> aggregates.
/// All queries are scoped by ProjectId for multi-tenant isolation.
/// </summary>
public interface IFileDocumentRepository
{
    /// <summary>
    /// Fetches a file by id within a project. Use for admin access
    /// where no user-scoping is required.
    /// </summary>
    Task<FileDocument?> GetByIdAsync(Guid id, Guid projectId, CancellationToken ct = default);

    /// <summary>
    /// Fetches a file by id within a project, scoped to a specific user.
    /// Use for regular (non-admin) callers.
    /// </summary>
    Task<FileDocument?> GetByIdAndUserAsync(Guid id, Guid projectId, Guid userId, CancellationToken ct = default);

    Task<(IReadOnlyList<FileDocument> Items, int TotalCount)> SearchAsync(
        Guid? projectId,
        string? keyword,
        Guid? userId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default);

    Task AddAsync(FileDocument document, CancellationToken ct = default);

    Task UpdateAsync(FileDocument document, CancellationToken ct = default);
}
