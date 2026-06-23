using DocumentStorage.Domain.Entities;

namespace DocumentStorage.Application.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AdminUser user, CancellationToken ct = default);
    Task UpdateAsync(AdminUser user, CancellationToken ct = default);
}
