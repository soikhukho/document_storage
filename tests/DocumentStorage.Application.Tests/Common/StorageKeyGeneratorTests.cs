using DocumentStorage.Application.Common;

namespace DocumentStorage.Application.Tests.Common;

public class StorageKeyGeneratorTests
{
    private static readonly Guid ProjectId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111");
    private static readonly Guid FileId = Guid.Parse("11112222-3333-4444-5555-666677778888");

    [Fact]
    public void Generate_ReturnsExpectedFormat()
    {
        var now = DateTime.UtcNow;

        var key = StorageKeyGenerator.Generate(ProjectId, UserId, FileId, "pdf");

        var expected = $"projects/{ProjectId}/users/{UserId}/{now:yyyy}/{now:MM}/{FileId}.pdf";
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Generate_StripsLeadingDotFromExtension()
    {
        var key = StorageKeyGenerator.Generate(ProjectId, UserId, FileId, ".PDF");

        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain("..pdf", key);
    }

    [Fact]
    public void Generate_AlwaysContainsProjectId()
    {
        var key = StorageKeyGenerator.Generate(ProjectId, UserId, FileId, "pdf");

        Assert.Contains($"projects/{ProjectId}/", key);
    }

    [Fact]
    public void Generate_AlwaysContainsUserId()
    {
        var key = StorageKeyGenerator.Generate(ProjectId, UserId, FileId, "pdf");

        Assert.Contains(UserId.ToString(), key);
    }

    [Fact]
    public void Generate_AlwaysContainsFileId()
    {
        var key = StorageKeyGenerator.Generate(ProjectId, UserId, FileId, "pdf");

        Assert.Contains(FileId.ToString(), key);
    }
}
