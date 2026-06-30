using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Queries;

public class GetTrashQueryHandler
    : IQueryHandler<GetTrashQuery, PagedResult<FileDto>>
{
    private readonly IFileDocumentRepository _repository;

    public GetTrashQueryHandler(IFileDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<FileDto>>> HandleAsync(
        GetTrashQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetTrashAsync(
            query.ProjectId,
            query.UserId,
            query.Keyword,
            query.Page,
            query.PageSize,
            query.SortBy,
            query.SortDirection,
            ct).ConfigureAwait(false);

        var dtos = items
            .Select(d => FileMapper.ToDto(d, string.Empty))
            .ToList();

        return Result<PagedResult<FileDto>>.Success(
            PagedResult<FileDto>.Create(dtos, query.Page, query.PageSize, totalCount));
    }
}
