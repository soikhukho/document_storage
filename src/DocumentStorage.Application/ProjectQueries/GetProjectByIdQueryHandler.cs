using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.ProjectCommands;
using DocumentStorage.Domain.Exceptions;

namespace DocumentStorage.Application.ProjectQueries;

public class GetProjectByIdQueryHandler
    : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _repository;

    public GetProjectByIdQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectDto> HandleAsync(
        GetProjectByIdQuery query, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, ct)
            ?? throw new ProjectNotFoundException(query.ProjectId);

        return CreateProjectCommandHandler.MapToDto(project);
    }
}
