using DocumentStorage.Application.Commands;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Commands;

public class DeleteFileCommandHandlerTests
{
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly DeleteFileCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public DeleteFileCommandHandlerTests()
    {
        _handler = new DeleteFileCommandHandler(_repo, _uow);
    }

    [Fact]
    public async Task HandleAsync_FileExists_SoftDeletesRecordButKeepsStorageObject()
    {
        var document = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 1024,
            "user/key/doc.pdf", StorageProviderType.Local, UserId);
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>()).Returns(document);

        var result = await _handler.HandleAsync(new DeleteFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsSuccess);
        Assert.True(document.IsDeleted);
        Assert.NotNull(document.DeletedAt);
        await _repo.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotFound_ReturnsFailureAndDoesNotCommit()
    {
        _repo.GetByIdAndUserAsync(FileId, ProjectId, UserId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        var result = await _handler.HandleAsync(new DeleteFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.FirstError!.Code);
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
    }
}
