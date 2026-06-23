using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentStorage.Infrastructure.Persistence;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly DocumentStorageDbContext _db;

    public AdminUserRepository(DocumentStorageDbContext db)
    {
        _db = db;
    }

    public Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => _db.AdminUsers.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AdminUsers.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(AdminUser user, CancellationToken ct = default)
        => await _db.AdminUsers.AddAsync(user, ct).ConfigureAwait(false);

    public Task UpdateAsync(AdminUser user, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _db.AdminUsers.Update(user);
        return Task.CompletedTask;
    }
}
