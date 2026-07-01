using DocumentStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentStorage.Infrastructure.Persistence;

public class DocumentStorageDbContext : DbContext
{
    public DbSet<FileDocument> FileDocuments => Set<FileDocument>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DocumentStorageDbContext(DbContextOptions<DocumentStorageDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentStorageDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
