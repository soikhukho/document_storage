using DocumentStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentStorage.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="FileDocument"/> to the FileDocuments table (SDD §12).
/// </summary>
public class FileDocumentConfiguration : IEntityTypeConfiguration<FileDocument>
{
    public void Configure(EntityTypeBuilder<FileDocument> builder)
    {
        builder.ToTable("FileDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Extension)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Size)
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Description)
            .IsRequired();

        builder.Property(x => x.UploadedBy)
            .IsRequired();

        builder.Property(x => x.ProjectId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.StorageKey)
            .HasDatabaseName("IX_FileDocuments_StorageKey");

        builder.HasIndex(x => x.UploadedBy)
            .HasDatabaseName("IX_FileDocuments_UploadedBy");

        builder.HasIndex(x => x.ProjectId)
            .HasDatabaseName("IX_FileDocuments_ProjectId");

        // Soft-delete global filter — deleted records are automatically excluded
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
