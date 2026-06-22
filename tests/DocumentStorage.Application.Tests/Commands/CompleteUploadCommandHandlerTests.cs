using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Enums;
using DocumentStorage.Domain.Exceptions;
using NSubstitute;
using InvalidFileTypeException = DocumentStorage.Domain.Exceptions.InvalidFileTypeException;
using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

namespace DocumentStorage.Application.Tests.Commands;

public class CompleteUploadCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly StorageOptions _options = new();
    private readonly CompleteUploadCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public CompleteUploadCommandHandlerTests()
    {
        _handler = new CompleteUploadCommandHandler(_storage, _repo, _uow, _options);
    }

    [Fact]
    public async Task HandleAsync_FileExistsInStorage_PersistsMetadata()
    {
        _storage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _storage.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("https://download/url");

        var command = new CompleteUploadCommand(ProjectId, FileId, "doc.pdf", "application/pdf", 1024, UserId, "desc");

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("doc.pdf", result.Name);
        Assert.Equal("pdf", result.Extension);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(1024, result.Size);
        Assert.Equal("desc", result.Description);
        Assert.Equal("https://download/url", result.DownloadUrl);

        await _repo.Received(1).AddAsync(Arg.Is<Domain.Entities.FileDocument>(d =>
            d.UploadedBy == UserId && d.ProjectId == ProjectId), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotInStorage_ThrowsFileNotFound()
    {
        _storage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new CompleteUploadCommand(ProjectId, FileId, "doc.pdf", "application/pdf", 1024, UserId);

        await Assert.ThrowsAsync<FileNotFoundException>(() => _handler.HandleAsync(command));

        await _repo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.FileDocument>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidFileType_ThrowsBeforeStorageCheck()
    {
        var command = new CompleteUploadCommand(ProjectId, FileId, "virus.exe", "application/octet-stream", 1024, UserId);

        await Assert.ThrowsAsync<InvalidFileTypeException>(() => _handler.HandleAsync(command));

        await _storage.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UsesConfiguredProviderType()
    {
        _options.Provider = StorageProviderType.S3;
        _storage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _storage.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("url");

        var command = new CompleteUploadCommand(ProjectId, FileId, "doc.pdf", "application/pdf", 1024, UserId);

        await _handler.HandleAsync(command);

        await _repo.Received(1).AddAsync(Arg.Is<Domain.Entities.FileDocument>(d =>
            d.Provider == StorageProviderType.S3), Arg.Any<CancellationToken>());
    }
}
