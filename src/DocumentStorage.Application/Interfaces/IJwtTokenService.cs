using DocumentStorage.Domain.Entities;

namespace DocumentStorage.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(AdminUser user);

    /// <summary>
    /// Validates the token signature and claims. Returns the admin user id
    /// when valid; null otherwise.
    /// </summary>
    Task<Guid?> ValidateTokenAsync(string token);
}
