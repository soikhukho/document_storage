using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Domain.Exceptions;

namespace DocumentStorage.Application.Tests.Common;

public class FileValidatorTests
{
    private readonly StorageOptions _options = new();

    [Theory]
    [InlineData("doc.pdf", "application/pdf", 100, "pdf")]
    [InlineData("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 100, "docx")]
    [InlineData("doc.XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 100, "xlsx")]
    [InlineData("image.png", "image/png", 100, "png")]
    [InlineData("image.JPG", "image/jpeg", 100, "jpg")]
    [InlineData("image.jpeg", "image/jpeg", 100, "jpeg")]
    public void ValidateAndExtractExtension_AllowedExtension_ReturnsLowercased(
        string name, string contentType, long size, string expected)
    {
        var result = FileValidator.ValidateAndExtractExtension(name, contentType, size, _options);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("file.exe", "application/octet-stream")]
    [InlineData("file.bat", "application/octet-stream")]
    [InlineData("file.dll", "application/octet-stream")]
    [InlineData("file.zip", "application/zip")]
    public void ValidateAndExtractExtension_DisallowedExtension_ThrowsInvalidFileType(
        string name, string contentType)
    {
        Assert.Throws<InvalidFileTypeException>(() =>
            FileValidator.ValidateAndExtractExtension(name, contentType, 100, _options));
    }

    [Fact]
    public void ValidateAndExtractExtension_ExceedsMaxSize_ThrowsArgument()
    {
        var oversized = _options.MaxUploadSizeBytes + 1;

        Assert.Throws<ArgumentException>(() =>
            FileValidator.ValidateAndExtractExtension("doc.pdf", "application/pdf", oversized, _options));
    }

    [Fact]
    public void ValidateAndExtractExtension_ExactlyAtMaxSize_Succeeds()
    {
        var maxSize = _options.MaxUploadSizeBytes;

        var result = FileValidator.ValidateAndExtractExtension("doc.pdf", "application/pdf", maxSize, _options);

        Assert.Equal("pdf", result);
    }

    [Theory]
    [InlineData("noextension")]
    [InlineData("file.")]
    public void ValidateAndExtractExtension_NoExtension_ThrowsArgument(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            FileValidator.ValidateAndExtractExtension(name, "application/octet-stream", 100, _options));
    }
}
