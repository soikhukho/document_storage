using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Auth;

/// <summary>
/// Seeds the default admin user on startup if it does not yet exist.
/// </summary>
public static class AdminUserSeeder
{
    private const string Username = "admin";
    private const string Password = "dip@123";

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(AdminUserSeeder));
        var users = sp.GetRequiredService<IAdminUserRepository>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var passwordHasher = sp.GetRequiredService<IPasswordHasher>();

        var existing = await users.GetByUsernameAsync(Username, ct).ConfigureAwait(false);
        if (existing is not null)
            return;

        var hash = passwordHasher.Hash(Password);
        var user = AdminUser.Create(Username, hash);
        await users.AddAsync(user, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Seeded bootstrap admin user '{Username}'.", Username);
    }
}
