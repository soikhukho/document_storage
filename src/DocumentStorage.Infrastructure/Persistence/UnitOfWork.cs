using DocumentStorage.Application.Interfaces;

namespace DocumentStorage.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly DocumentStorageDbContext _db;

    public UnitOfWork(DocumentStorageDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
