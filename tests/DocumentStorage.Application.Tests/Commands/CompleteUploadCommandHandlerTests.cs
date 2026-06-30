using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Commands;

public class CompleteUploadCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly StorageOptions _options = new();
    private readonly CompleteUploadCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public CompleteUploadCommandHandlerTests()
    {
        _projectRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Project.Create("Test Project", "test-project"));
        _handler = new CompleteUploadCommandHandler(_storage, _repo, _projectRepo, _uow, _options);
    }

    [Fact]
    public async Task HandleAsync_FileExistsInStorage_PersistsMetadata()
    {
        _storage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _storage.GetDownloadUrlAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns("https://download/url");

        var command = new CompleteUploadCommand(ProjectId, FileId, "doc.pdf", "application/pdf", 1024, UserId, "desc");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.Id);
        Assert.Equal("doc.pdf", result.Value.Name);
        Assert.Equal("pdf", result.Value.Extension);
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.Equal(1024, result.Value.Size);
        Assert.Equal("desc", result.Value.Description);
        Assert.Equal("https://download/url", result.Value.DownloadUrl);

        await _repo.Received(1).AddAsync(Arg.Is<FileDocument>(d =>
            d.UploadedBy == UserId && d.ProjectId == ProjectId), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotInStorage_ReturnsNotFoundFailure()
    {
        _storage.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = new CompleteUploadCommand(ProjectId, FileId, "doc.pdf", "application/pdf", 1024, UserId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.FirstError!.Code);
        await _repo.DidNotReceive().AddAsync(Arg.Any<FileDocument>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidFileType_ReturnsValidationFailureBeforeStorageCheck()
    {
        var command = new CompleteUploadCommand(ProjectId, FileId, "virus.exe", "application/octet-stream", 1024, UserId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_FILE_TYPE", result.FirstError!.Code);
        await _storage.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProjectNotFound_ReturnsNotFound()
    {
        _projectRepo.GetByIdAsync(ProjectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var command = new CompleteUploadCommand(ProjectId, FileId, "doc.pdf", "application/pdf", 1024, UserId);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("PROJECT_NOT_FOUND", result.FirstError!.Code);
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

        await _repo.Received(1).AddAsync(Arg.Is<FileDocument>(d =>
            d.Provider == StorageProviderType.S3), Arg.Any<CancellationToken>());
    }
}
