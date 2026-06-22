using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.ProjectCommands;

namespace DocumentStorage.Application.ProjectQueries;

public class GetAllProjectsQueryHandler
    : IQueryHandler<GetAllProjectsQuery, PagedResult<ProjectDto>>
{
    private readonly IProjectRepository _repository;

    public GetAllProjectsQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<ProjectDto>> HandleAsync(
        GetAllProjectsQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetAllAsync(
            query.Page, query.PageSize, ct).ConfigureAwait(false);

        var dtos = items
            .Select(CreateProjectCommandHandler.MapToDto)
            .ToList();

        return PagedResult<ProjectDto>.Create(dtos, query.Page, query.PageSize, totalCount);
    }
}
