using DocumentStorage.Application.Commands;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Commands;

public class DeleteFileCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly DeleteFileCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public DeleteFileCommandHandlerTests()
    {
        _handler = new DeleteFileCommandHandler(_storage, _repo, _uow);
    }

    [Fact]
    public async Task HandleAsync_FileExists_DeletesFromStorageAndSoftDeletesRecord()
    {
        var document = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 1024,
            "user/key/doc.pdf", StorageProviderType.Local, UserId);
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.HandleAsync(new DeleteFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsSuccess);
        await _storage.Received(1).DeleteAsync(document.StorageKey, Arg.Any<CancellationToken>());
        Assert.True(document.IsDeleted);
        Assert.NotNull(document.DeletedAt);
        await _repo.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotFound_ReturnsFailureAndDoesNotDeleteFromStorage()
    {
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        var result = await _handler.HandleAsync(new DeleteFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.FirstError!.Code);
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OtherUsersFile_ReturnsFailureFromRepo()
    {
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        var result = await _handler.HandleAsync(new DeleteFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.FirstError!.Code);
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
