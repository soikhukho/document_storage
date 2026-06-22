using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.Queries;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Queries;

public class GetFilesByUserQueryHandlerTests
{
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly GetFilesByUserQueryHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public GetFilesByUserQueryHandlerTests()
    {
        _handler = new GetFilesByUserQueryHandler(_repo);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUserFilesPaged()
    {
        var documents = new List<FileDocument>
        {
            FileDocument.Create(ProjectId, "a.pdf", "pdf", "application/pdf", 100, "k1", StorageProviderType.Local, UserId),
            FileDocument.Create(ProjectId, "b.pdf", "pdf", "application/pdf", 200, "k2", StorageProviderType.Local, UserId),
            FileDocument.Create(ProjectId, "c.pdf", "pdf", "application/pdf", 300, "k3", StorageProviderType.Local, UserId),
        };

        _repo.SearchAsync(ProjectId, null, UserId, Arg.Any<int>(), Arg.Any<int>(),
            null, null, Arg.Any<CancellationToken>())
            .Returns((documents, 3));

        var result = await _handler.HandleAsync(
            new GetFilesByUserQuery(ProjectId, UserId, Page: 1, PageSize: 10));

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.All(result.Items, dto => Assert.Equal(string.Empty, dto.DownloadUrl));
    }

    [Fact]
    public async Task HandleAsync_PassesUserIdAndPagingToRepository()
    {
        _repo.SearchAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileDocument>(), 0));

        await _handler.HandleAsync(
            new GetFilesByUserQuery(ProjectId, UserId, Page: 2, PageSize: 5));

        await _repo.Received(1).SearchAsync(
            ProjectId, null, UserId, 2, 5, null, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoFiles_ReturnsEmptyResult()
    {
        _repo.SearchAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileDocument>(), 0));

        var result = await _handler.HandleAsync(new GetFilesByUserQuery(ProjectId, UserId));

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }
}
