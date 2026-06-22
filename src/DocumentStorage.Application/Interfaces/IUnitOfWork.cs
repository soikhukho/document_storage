namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Unit of Work — commits pending changes across repositories in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
