using DocumentStorage.Application.Commands;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DocumentStorage.Application.Tests.Commands;

public class PurgeFileCommandHandlerTests
{
    private readonly IStorageProvider _storage = Substitute.For<IStorageProvider>();
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PurgeFileCommandHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public PurgeFileCommandHandlerTests()
    {
        _handler = new PurgeFileCommandHandler(_storage, _repo, _uow);
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
    public async Task HandleAsync_FileInTrash_NoOtherRefs_DeletesStorageAndRemovesRow()
    {
        var doc = SoftDeletedDoc();
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>()).Returns(doc);
        _repo.CountOtherReferencesByStorageKeyAsync(doc.StorageKey, doc.Id, Arg.Any<CancellationToken>())
            .Returns(0);

        var result = await _handler.HandleAsync(new PurgeFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsSuccess);
        await _storage.Received(1).DeleteAsync(doc.StorageKey, Arg.Any<CancellationToken>());
        await _repo.Received(1).HardRemoveAsync(doc, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileInTrash_OtherRefsShareStorageKey_SkipsStorageDeleteButRemovesRow()
    {
        var doc = SoftDeletedDoc();
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>()).Returns(doc);
        _repo.CountOtherReferencesByStorageKeyAsync(doc.StorageKey, doc.Id, Arg.Any<CancellationToken>())
            .Returns(1); // another record (active or trash) holds the same storage key

        var result = await _handler.HandleAsync(new PurgeFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsSuccess);
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).HardRemoveAsync(doc, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FileNotInTrash_ReturnsNotFound()
    {
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>())
            .Returns((FileDocument?)null);

        var result = await _handler.HandleAsync(new PurgeFileCommand(ProjectId, FileId, UserId));

        Assert.True(result.IsFailure);
        Assert.Equal("FILE_NOT_FOUND", result.FirstError!.Code);
        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().HardRemoveAsync(Arg.Any<FileDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_StorageDeleteFails_DoesNotRemoveRow()
    {
        var doc = SoftDeletedDoc();
        _repo.GetDeletedByIdAsync(FileId, ProjectId, Arg.Any<CancellationToken>()).Returns(doc);
        _repo.CountOtherReferencesByStorageKeyAsync(doc.StorageKey, doc.Id, Arg.Any<CancellationToken>())
            .Returns(0);
        _storage.DeleteAsync(doc.StorageKey, Arg.Any<CancellationToken>())
            .Throws(new Exception("S3 down"));

        await Assert.ThrowsAsync<Exception>(() =>
            _handler.HandleAsync(new PurgeFileCommand(ProjectId, FileId, UserId)));

        await _repo.DidNotReceive().HardRemoveAsync(Arg.Any<FileDocument>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
