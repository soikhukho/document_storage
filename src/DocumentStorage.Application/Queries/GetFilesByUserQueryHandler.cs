using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Queries;

public class GetFilesByUserQueryHandler
    : IQueryHandler<GetFilesByUserQuery, PagedResult<FileDto>>
{
    private readonly IFileDocumentRepository _repository;

    public GetFilesByUserQueryHandler(IFileDocumentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<FileDto>>> HandleAsync(
        GetFilesByUserQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.SearchAsync(
            query.ProjectId, null, query.UserId, query.Page, query.PageSize, null, null, ct).ConfigureAwait(false);

        var dtos = items
            .Select(d => FileMapper.ToDto(d, string.Empty))
            .ToList();

        return Result<PagedResult<FileDto>>.Success(
            PagedResult<FileDto>.Create(dtos, query.Page, query.PageSize, totalCount));
    }
}
