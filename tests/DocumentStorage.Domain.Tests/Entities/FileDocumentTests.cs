using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using DocumentStorage.Domain.Exceptions;

namespace DocumentStorage.Domain.Tests.Entities;

public class FileDocumentTests
{
    private static readonly Guid ValidUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ValidProjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static FileDocument CreateValidDocument() => FileDocument.Create(
        ValidProjectId,
        "report.pdf",
        "pdf",
        "application/pdf",
        1024,
        "user/abc/2026/06/file.pdf",
        StorageProviderType.Local,
        ValidUserId,
        "A test file");

    // ── Create: success cases ──

    [Fact]
    public void Create_WithValidParameters_ReturnsDocumentWithCorrectValues()
    {
        var doc = CreateValidDocument();

        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.Equal(ValidProjectId, doc.ProjectId);
        Assert.Equal("report.pdf", doc.Name);
        Assert.Equal("pdf", doc.Extension);
        Assert.Equal("application/pdf", doc.ContentType);
        Assert.Equal(1024, doc.Size);
        Assert.Equal("user/abc/2026/06/file.pdf", doc.StorageKey);
        Assert.Equal(StorageProviderType.Local, doc.Provider);
        Assert.Equal(ValidUserId, doc.UploadedBy);
        Assert.Equal("A test file", doc.Description);
        Assert.False(doc.IsDeleted);
        Assert.Null(doc.DeletedAt);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var doc1 = CreateValidDocument();
        var doc2 = CreateValidDocument();

        Assert.NotEqual(doc1.Id, doc2.Id);
    }

    [Fact]
    public void Create_NormalizesExtension_StripsLeadingDotAndLowercases()
    {
        var doc = FileDocument.Create(
            ValidProjectId, "file.PDF", ".PDF", "application/pdf", 100,
            "key", StorageProviderType.Local, ValidUserId);

        Assert.Equal("pdf", doc.Extension);
    }

    [Fact]
    public void Create_TrimsWhitespaceFromNameAndContentType()
    {
        var doc = FileDocument.Create(
            ValidProjectId, "  report.pdf  ", "pdf", "  application/pdf  ", 100,
            "key", StorageProviderType.Local, ValidUserId);

        Assert.Equal("report.pdf", doc.Name);
        Assert.Equal("application/pdf", doc.ContentType);
    }

    [Fact]
    public void Create_WithNullDescription_DefaultsToEmpty()
    {
        var doc = FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", "application/pdf", 100,
            "key", StorageProviderType.Local, ValidUserId, null);

        Assert.Equal(string.Empty, doc.Description);
    }

    [Fact]
    public void Create_WithWhitespaceDescription_DefaultsToEmpty()
    {
        var doc = FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", "application/pdf", 100,
            "key", StorageProviderType.Local, ValidUserId, "   ");

        Assert.Equal(string.Empty, doc.Description);
    }

    [Fact]
    public void Create_SetsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var doc = CreateValidDocument();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.True(doc.CreatedAt >= before);
        Assert.True(doc.CreatedAt <= after);
    }

    // ── Create: validation failures ──

    [Fact]
    public void Create_WithEmptyProjectId_Throws()
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            Guid.Empty, "file.pdf", "pdf", "application/pdf", 100,
            "key", StorageProviderType.Local, ValidUserId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            ValidProjectId, name!, "pdf", "application/pdf", 100,
            "key", StorageProviderType.Local, ValidUserId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidExtension_Throws(string? ext)
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            ValidProjectId, "file.pdf", ext!, "application/pdf", 100,
            "key", StorageProviderType.Local, ValidUserId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidContentType_Throws(string? ct)
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", ct!, 100,
            "key", StorageProviderType.Local, ValidUserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Create_WithNonPositiveSize_Throws(long size)
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", "application/pdf", size,
            "key", StorageProviderType.Local, ValidUserId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidStorageKey_Throws(string? key)
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", "application/pdf", 100,
            key!, StorageProviderType.Local, ValidUserId));
    }

    [Fact]
    public void Create_WithEmptyUploadedBy_Throws()
    {
        Assert.Throws<ArgumentException>(() => FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", "application/pdf", 100,
            "key", StorageProviderType.Local, Guid.Empty));
    }

    [Fact]
    public void Create_WithInvalidProviderEnum_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileDocument.Create(
            ValidProjectId, "file.pdf", "pdf", "application/pdf", 100,
            "key", (StorageProviderType)999, ValidUserId));
    }

    // ── SoftDelete ──

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var doc = CreateValidDocument();

        doc.SoftDelete();

        Assert.True(doc.IsDeleted);
        Assert.NotNull(doc.DeletedAt);
    }

    [Fact]
    public void SoftDelete_IsIdempotent_DoesNotUpdateDeletedAtTwice()
    {
        var doc = CreateValidDocument();
        doc.SoftDelete();
        var firstDeletedAt = doc.DeletedAt;

        Thread.Sleep(10);
        doc.SoftDelete();

        Assert.Equal(firstDeletedAt, doc.DeletedAt);
    }

    // ── Restore ──

    [Fact]
    public void Restore_ClearsIsDeletedAndDeletedAt()
    {
        var doc = CreateValidDocument();
        doc.SoftDelete();

        doc.Restore();

        Assert.False(doc.IsDeleted);
        Assert.Null(doc.DeletedAt);
    }

    // ── UpdateDescription ──

    [Fact]
    public void UpdateDescription_SetsNewDescription()
    {
        var doc = CreateValidDocument();

        doc.UpdateDescription("Updated description");

        Assert.Equal("Updated description", doc.Description);
    }

    [Fact]
    public void UpdateDescription_WithNull_SetsEmptyString()
    {
        var doc = CreateValidDocument();

        doc.UpdateDescription(null);

        Assert.Equal(string.Empty, doc.Description);
    }

    [Fact]
    public void UpdateDescription_TrimsWhitespace()
    {
        var doc = CreateValidDocument();

        doc.UpdateDescription("  spaced  ");

        Assert.Equal("spaced", doc.Description);
    }
}
