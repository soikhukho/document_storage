using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.ProjectCommands;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.ProjectQueries;

public class GetProjectByIdQueryHandler
    : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _repository;

    public GetProjectByIdQueryHandler(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProjectDto>> HandleAsync(
        GetProjectByIdQuery query, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(query.ProjectId, ct).ConfigureAwait(false);

        if (project is null)
            return Result<ProjectDto>.Failure(
                AppError.NotFound("PROJECT_NOT_FOUND", $"Project with id '{query.ProjectId}' was not found."));

        return Result<ProjectDto>.Success(CreateProjectCommandHandler.MapToDto(project));
    }
}
