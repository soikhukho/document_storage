using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;

namespace DocumentStorage.Application.Queries;

public class SearchFilesQueryHandler
    : IQueryHandler<SearchFilesQuery, PagedResult<FileDto>>
{
    private readonly IFileDocumentRepository _repository;

    public SearchFilesQueryHandler(IFileDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<FileDto>> HandleAsync(
        SearchFilesQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.ProjectId,
            query.Keyword,
            query.UserId,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortDirection,
            ct).ConfigureAwait(false);

        // DownloadUrl is intentionally empty for list results.
        // Clients should call GET /api/files/{id} for a fresh presigned download URL.
        var dtos = items
            .Select(d => FileMapper.ToDto(d, string.Empty))
            .ToList();

        return PagedResult<FileDto>.Create(dtos, query.Page, query.PageSize, totalCount);
    }
}
