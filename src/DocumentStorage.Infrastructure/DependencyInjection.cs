using Amazon.S3;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Enums;
using DocumentStorage.Infrastructure.Auth;
using DocumentStorage.Infrastructure.Caching;
using DocumentStorage.Infrastructure.Logging;
using DocumentStorage.Infrastructure.Persistence;
using DocumentStorage.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentStorage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── Storage options (SDD §18) ──
        var storageSection = configuration.GetSection(StorageOptions.SectionName);
        var storageOptions = storageSection.Get<StorageOptions>() ?? new StorageOptions();

        // Explicitly read Provider as string to handle enum conversion
        var providerString = storageSection["Provider"];
        if (!string.IsNullOrEmpty(providerString)
            && Enum.TryParse<StorageProviderType>(providerString, ignoreCase: true, out var providerEnum))
        {
            storageOptions.Provider = providerEnum;
        }

        services.AddSingleton(storageOptions);

        // ── EF Core (PostgreSQL or SQL Server) ──
        var dbProvider = configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";
        var connectionString = configuration.GetConnectionString(dbProvider)
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<DocumentStorageDbContext>(options =>
        {
            if (IsSqlServer(dbProvider))
                options.UseSqlServer(connectionString);
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IFileDocumentRepository, FileDocumentRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Audit logging (uses its own scope per entry) ──
        services.AddSingleton<IAuditLogger, AuditLogger>();

        // ── Auth (admin JWT login) ──
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // ── Caching ──
        services.AddMemoryCache(options => options.SizeLimit = 10_000);
        services.AddSingleton<IProjectCache, ProjectCache>();

        // ── Current user context ──
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<ICurrentProjectContext, CurrentProjectContext>();

        // ── Storage provider (resolved at startup from config) ──
        switch (storageOptions.Provider)
        {
            case StorageProviderType.S3:
                services.AddSingleton(configuration.GetSection("S3").Get<S3StorageOptions>()
                    ?? new S3StorageOptions());
                services.AddSingleton<IStorageProvider, S3StorageProvider>();
                break;

            case StorageProviderType.MinIO:
                services.AddSingleton(configuration.GetSection("MinIO").Get<MinioStorageOptions>()
                    ?? new MinioStorageOptions());
                services.AddSingleton<IStorageProvider, MinioStorageProvider>();
                break;

            default:
                services.AddSingleton(configuration.GetSection("Local").Get<LocalStorageOptions>()
                    ?? new LocalStorageOptions());
                services.AddSingleton<IStorageProvider, LocalStorageProvider>();
                break;
        }

        return services;
    }

    private static bool IsSqlServer(string provider)
        => provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
}
