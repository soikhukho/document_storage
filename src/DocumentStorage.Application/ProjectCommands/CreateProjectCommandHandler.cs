using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.ProjectCommands;

public class CreateProjectCommandHandler
    : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectDto>> HandleAsync(
        CreateProjectCommand command, CancellationToken ct = default)
    {
        Project project;
        try
        {
            project = Project.Create(command.Name, command.FolderName, command.Description);
        }
        catch (ArgumentException ex)
        {
            return Result<ProjectDto>.Failure(
                AppError.Validation("INVALID_PROJECT", ex.Message));
        }

        if (await _repository.ExistsByFolderNameAsync(project.FolderName, excludeId: null, ct).ConfigureAwait(false))
        {
            return Result<ProjectDto>.Failure(
                AppError.Conflict(
                    "FOLDER_NAME_EXISTS",
                    $"Folder name '{project.FolderName}' is already used by another project."));
        }

        await _repository.AddAsync(project, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<ProjectDto>.Success(MapToDto(project));
    }

    internal static ProjectDto MapToDto(Project project) => new(
        project.Id,
        project.Name,
        project.FolderName,
        project.Description,
        project.ApiKey,
        project.IsActive,
        project.CreatedAt);
}
