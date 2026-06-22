using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;
using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

namespace DocumentStorage.Application.Tests.Commands;

public class UpdateDescriptionCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly StorageOptions _options = new();
    private readonly UpdateDescriptionCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public UpdateDescriptionCommandHandlerTests()
    {
        _handler = new UpdateDescriptionCommandHandler(_storage, _repo, _uow, _options);
    }

    [Fact]
    public async Task HandleAsync_FileExists_UpdatesDescriptionAndReturnsDto()
    {
        var document = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 1024,
            "key", StorageProviderType.Local, UserId, "old desc");

        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>()).Returns(document);
        _storage.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("https://download/url");

        var result = await _handler.HandleAsync(
            new UpdateDescriptionCommand(ProjectId, FileId, UserId, "new desc"));

        Assert.Equal("new desc", document.Description);
        Assert.Equal("new desc", result.Description);
        Assert.Equal("https://download/url", result.DownloadUrl);

        await _repo.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotFound_Throws()
    {
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _handler.HandleAsync(new UpdateDescriptionCommand(ProjectId, FileId, UserId, "desc")));

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NullDescription_SetsEmptyString()
    {
        var document = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 1024,
            "key", StorageProviderType.Local, UserId, "old");

        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>()).Returns(document);
        _storage.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("url");

        await _handler.HandleAsync(
            new UpdateDescriptionCommand(ProjectId, FileId, UserId, null!));

        Assert.Equal(string.Empty, document.Description);
    }
}
