using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.Queries;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;
using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

namespace DocumentStorage.Application.Tests.Queries;

public class GetFileByIdQueryHandlerTests
{
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly StorageOptions _options = new();
    private readonly GetFileByIdQueryHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public GetFileByIdQueryHandlerTests()
    {
        _handler = new GetFileByIdQueryHandler(_repo, _storage, _options);
    }

    [Fact]
    public async Task HandleAsync_FileExists_ReturnsDtoWithDownloadUrl()
    {
        var document = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 1024,
            "key", StorageProviderType.Local, UserId, "test");

        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>()).Returns(document);
        _storage.GetDownloadUrlAsync("key", _options.DownloadExpirationMinutes, Arg.Any<CancellationToken>())
            .Returns("https://download/url");

        var result = await _handler.HandleAsync(new GetFileByIdQuery(ProjectId, FileId, UserId));

        Assert.Equal(document.Id, result.Id);
        Assert.Equal("doc.pdf", result.Name);
        Assert.Equal("https://download/url", result.DownloadUrl);
        Assert.Equal("test", result.Description);
    }

    [Fact]
    public async Task HandleAsync_FileNotFound_Throws()
    {
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _handler.HandleAsync(new GetFileByIdQuery(ProjectId, FileId, UserId)));

        await _storage.DidNotReceive().GetDownloadUrlAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UsesConfiguredDownloadExpiration()
    {
        var document = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 100,
            "key", StorageProviderType.Local, UserId);

        _repo.GetByIdAndUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(document);
        _storage.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("url");

        await _handler.HandleAsync(new GetFileByIdQuery(ProjectId, FileId, UserId));

        await _storage.Received(1).GetDownloadUrlAsync(
            "key", _options.DownloadExpirationMinutes, Arg.Any<CancellationToken>());
    }
}
