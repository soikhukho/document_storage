using DocumentStorage.Application.Common;

namespace DocumentStorage.Application.Tests.Common;

public class StorageKeyGeneratorTests
{
    [Fact]
    public void Generate_ReturnsProjectSlashFileFormat()
    {
        var key = StorageKeyGenerator.Generate("Test Project", "report.pdf");

        Assert.Equal("Test Project/report.pdf", key);
    }

    [Fact]
    public void Generate_PreservesVietnameseAndSpaces()
    {
        var key = StorageKeyGenerator.Generate("Sky Lake Mỹ Đình", "tài liệu.pdf");

        Assert.Equal("Sky Lake Mỹ Đình/tài liệu.pdf", key);
    }

    [Fact]
    public void Generate_ReplacesSlashInProjectName()
    {
        var key = StorageKeyGenerator.Generate("a/b\\c", "file.pdf");

        Assert.Equal("a_b_c/file.pdf", key);
    }

    [Fact]
    public void Generate_ReplacesSlashInFileName()
    {
        var key = StorageKeyGenerator.Generate("Proj", "sub/dir/file.pdf");

        Assert.Equal("Proj/sub_dir_file.pdf", key);
    }

    [Fact]
    public void Generate_FallsBackToUnderscore_WhenProjectNameEmpty()
    {
        var key = StorageKeyGenerator.Generate("   ", "file.pdf");

        Assert.Equal("_/file.pdf", key);
    }

    [Fact]
    public void Generate_FallsBackToUnderscore_WhenFileNameEmpty()
    {
        var key = StorageKeyGenerator.Generate("Proj", "");

        Assert.Equal("Proj/_", key);
    }
}
