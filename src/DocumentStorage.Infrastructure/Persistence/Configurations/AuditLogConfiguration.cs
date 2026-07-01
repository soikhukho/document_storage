using DocumentStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentStorage.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.HttpMethod)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Path)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.StatusCode)
            .IsRequired();

        builder.Property(x => x.Success)
            .IsRequired();

        builder.Property(x => x.ActorType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ActorId)
            .HasMaxLength(100);

        builder.Property(x => x.ProjectId);

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);

        builder.Property(x => x.IPAddress)
            .HasMaxLength(100);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.Details)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(x => x.ProjectId)
            .HasDatabaseName("IX_AuditLogs_ProjectId");

        builder.HasIndex(x => x.ActorType)
            .HasDatabaseName("IX_AuditLogs_ActorType");
    }
}
