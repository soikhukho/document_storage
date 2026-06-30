using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.ProjectCommands;

public class UpdateProjectCommandHandler
    : ICommandHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectDto>> HandleAsync(
        UpdateProjectCommand command, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, ct).ConfigureAwait(false);

        if (project is null)
            return Result<ProjectDto>.Failure(
                AppError.NotFound("PROJECT_NOT_FOUND", $"Project with id '{command.ProjectId}' was not found."));

        try
        {
            project.Update(command.Name, command.Description);
        }
        catch (ArgumentException ex)
        {
            return Result<ProjectDto>.Failure(
                AppError.Validation("INVALID_PROJECT", ex.Message));
        }

        await _repository.UpdateAsync(project, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<ProjectDto>.Success(CreateProjectCommandHandler.MapToDto(project));
    }
}
