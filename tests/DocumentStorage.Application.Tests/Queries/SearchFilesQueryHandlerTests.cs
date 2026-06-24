using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.Queries;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Enums;
using NSubstitute;

namespace DocumentStorage.Application.Tests.Queries;

public class SearchFilesQueryHandlerTests
{
    private readonly IFileDocumentRepository _repo = Substitute.For<IFileDocumentRepository>();
    private readonly SearchFilesQueryHandler _handler;

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public SearchFilesQueryHandlerTests()
    {
        _handler = new SearchFilesQueryHandler(_repo);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPagedResultWithEmptyDownloadUrls()
    {
        var documents = new List<FileDocument>
        {
            FileDocument.Create(ProjectId, "a.pdf", "pdf", "application/pdf", 100, "k1", StorageProviderType.Local, UserId),
            FileDocument.Create(ProjectId, "b.pdf", "pdf", "application/pdf", 200, "k2", StorageProviderType.Local, UserId),
        };

        _repo.SearchAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((documents, 50));

        var result = await _handler.HandleAsync(
            new SearchFilesQuery(ProjectId, "a", UserId, Page: 1, PageSize: 20));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.Equal(50, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.All(result.Value.Items, dto => Assert.Equal(string.Empty, dto.DownloadUrl));
    }

    [Fact]
    public async Task HandleAsync_NoResults_ReturnsEmptyPage()
    {
        _repo.SearchAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileDocument>(), 0));

        var result = await _handler.HandleAsync(
            new SearchFilesQuery(ProjectId, null, UserId));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_PassesAllParametersToRepository()
    {
        _repo.SearchAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileDocument>(), 0));

        await _handler.HandleAsync(
            new SearchFilesQuery(ProjectId, "keyword", UserId, Page: 3, PageSize: 5, SortBy: "name", SortDirection: "desc"));

        await _repo.Received(1).SearchAsync(
            ProjectId, "keyword", UserId, 3, 5, "name", "desc", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNullProjectId_SearchesAllProjects()
    {
        _repo.SearchAsync(Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((new List<FileDocument>(), 0));

        await _handler.HandleAsync(
            new SearchFilesQuery(null, null, null));

        await _repo.Received(1).SearchAsync(
            Arg.Is<Guid?>(x => x == null),
            Arg.Is<string?>(x => x == null),
            Arg.Is<Guid?>(x => x == null),
            Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
