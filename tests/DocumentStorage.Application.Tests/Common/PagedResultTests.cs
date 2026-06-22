using DocumentStorage.Application.DTOs;

namespace DocumentStorage.Application.Tests.Common;

public class PagedResultTests
{
    [Fact]
    public void Create_ComputesTotalPagesCorrectly()
    {
        var items = new List<int> { 1, 2, 3 };

        var result = PagedResult<int>.Create(items, page: 1, pageSize: 10, totalCount: 25);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void Create_WithExactDivision_NoExtraPage()
    {
        var result = PagedResult<int>.Create(
            new List<int>(), page: 1, pageSize: 20, totalCount: 100);

        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public void Create_WithZeroTotal_ReturnsZeroPages()
    {
        var result = PagedResult<int>.Create(
            new List<int>(), page: 1, pageSize: 10, totalCount: 0);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Create_WithZeroPageSize_ReturnsZeroPages()
    {
        var result = PagedResult<int>.Create(
            new List<int>(), page: 1, pageSize: 0, totalCount: 10);

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Create_RemainingItemsProducesExtraPage()
    {
        // 55 items, 10 per page → 6 pages
        var result = PagedResult<int>.Create(
            new List<int>(), page: 1, pageSize: 10, totalCount: 55);

        Assert.Equal(6, result.TotalPages);
    }

    [Fact]
    public void Create_PreservesItemsPageAndPageSize()
    {
        var items = new List<string> { "a", "b" };

        var result = PagedResult<string>.Create(items, page: 3, pageSize: 5, totalCount: 20);

        Assert.Same(items, result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(20, result.TotalCount);
    }
}
