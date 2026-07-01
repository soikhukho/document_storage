using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocumentStorage.Infrastructure.Logging;

/// <summary>
/// Persists audit entries using a dedicated DI scope so that audit data
/// is saved even when the request's primary DbContext fails or rolls back.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(IServiceScopeFactory scopeFactory, ILogger<AuditLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        try
        {
            var log = AuditLog.Create(
                entry.HttpMethod, entry.Path, entry.Action, entry.StatusCode,
                entry.ActorType, entry.ActorId, entry.ProjectId, entry.EntityId,
                entry.IPAddress, entry.UserAgent, entry.Details);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DocumentStorageDbContext>();
            db.AuditLogs.Add(log);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist audit log: {Method} {Path} ({StatusCode})",
                entry.HttpMethod, entry.Path, entry.StatusCode);
        }
    }
}
