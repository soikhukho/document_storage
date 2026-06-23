using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Auth;

/// <summary>
/// Seeds a bootstrap admin user on startup when one is configured via
/// <c>Auth:BootstrapAdminUsername</c> / <c>Auth:BootstrapAdminPassword</c>
/// and no admin user with that username exists yet.
///
/// Set both values to empty to disable seeding.
/// </summary>
public static class AdminUserSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var configuration = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(AdminUserSeeder));
        var users = sp.GetRequiredService<IAdminUserRepository>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher>();

        var username = configuration["Auth:BootstrapAdminUsername"];
        var password = configuration["Auth:BootstrapAdminPassword"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return;

        var existing = await users.GetByUsernameAsync(username, ct).ConfigureAwait(false);
        if (existing is not null)
            return;

        var hash = passwordHasher.Hash(password);
        var user = AdminUser.Create(username, hash);
        await users.AddAsync(user, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Seeded bootstrap admin user '{Username}'.", username);
    }
}
