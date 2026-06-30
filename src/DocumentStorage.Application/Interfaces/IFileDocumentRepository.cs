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

    /// <summary>
    /// Returns true if a non-deleted file with the given name already exists in the project.
    /// Used to block duplicate uploads before issuing a presigned URL.
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid projectId, string name, CancellationToken ct = default);

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

    /// <summary>
    /// Fetches a soft-deleted file by id within a project. Bypasses the global
    /// IsDeleted query filter. Used by trash endpoints (restore / purge).
    /// </summary>
    Task<FileDocument?> GetDeletedByIdAsync(Guid id, Guid projectId, CancellationToken ct = default);

    /// <summary>
    /// Paged list of soft-deleted files in a project (the project's trash bin).
    /// </summary>
    Task<(IReadOnlyList<FileDocument> Items, int TotalCount)> GetTrashAsync(
        Guid projectId,
        Guid? userId,
        string? keyword,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken ct = default);

    /// <summary>
    /// Permanently removes the row from the database (bypasses soft-delete).
    /// Used by the purge endpoint after the S3 object has been deleted.
    /// </summary>
    Task HardRemoveAsync(FileDocument document, CancellationToken ct = default);

    /// <summary>
    /// Counts how many OTHER rows (active or soft-deleted) reference the same
    /// storage key, excluding the given id. Used by purge to decide whether the
    /// S3 object can be safely deleted or is still referenced by another record.
    /// </summary>
    Task<int> CountOtherReferencesByStorageKeyAsync(
        string storageKey, Guid excludeId, CancellationToken ct = default);
}
