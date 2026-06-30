using DocumentStorage.Application.Commands;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Commands;

public class RestoreFileCommandHandlerTests
{
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RestoreFileCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public RestoreFileCommandHandlerTests()
    {
        _handler = new RestoreFileCommandHandler(_repo, _uow);
    }

    private static FileDocument SoftDeletedDoc()
    {
        var doc = FileDocument.Create(
            ProjectId, "doc.pdf", "pdf", "application/pdf", 1024,
            "Test_Project/doc.pdf", StorageProviderType.Local, UserId);
        doc.GetType().GetMethod("SoftDelete")!.Invoke(doc, null);
        return doc;
    }

    [Fact]
    public async Task HandleAsync_FileInTrash_RestoresSuccessfully()
    {
        var doc = SoftDeletedDoc();
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>()).Returns(doc);
        _repo.ExistsByNameAsync(ProjectId, doc.Name, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.HandleAsync(new RestoreFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsSuccess);
        Assert.False(doc.IsDeleted);
        Assert.Null(doc.DeletedAt);
        await _repo.Received(1).UpdateAsync(doc, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotInTrash_ReturnsNotFound()
    {
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        var result = await _handler.HandleAsync(new RestoreFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.FirstError!.Code);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActiveFileWithNameExists_ReturnsConflict()
    {
        var doc = SoftDeletedDoc();
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>()).Returns(doc);
        _repo.ExistsByNameAsync(ProjectId, doc.Name, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(new RestoreFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NAME_EXISTS", result.FirstError!.Code);
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
