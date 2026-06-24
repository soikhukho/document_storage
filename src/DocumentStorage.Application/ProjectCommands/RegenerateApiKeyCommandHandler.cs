using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.ProjectCommands;

public class RegenerateApiKeyCommandHandler
    : ICommandHandler<RegenerateApiKeyCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectCache _cache;

    public RegenerateApiKeyCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IProjectCache cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result<ProjectDto>> HandleAsync(
        RegenerateApiKeyCommand command, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, ct).ConfigureAwait(false);

        if (project is null)
            return Result<ProjectDto>.Failure(
                AppError.NotFound("PROJECT_NOT_FOUND", $"Project with id '{command.ProjectId}' was not found."));

        var oldApiKey = project.ApiKey;

        project.RegenerateApiKey();
        await _repository.UpdateAsync(project, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _cache.Invalidate(oldApiKey);

        return Result<ProjectDto>.Success(CreateProjectCommandHandler.MapToDto(project));
    }
}
