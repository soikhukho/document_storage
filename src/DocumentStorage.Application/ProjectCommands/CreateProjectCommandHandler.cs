using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;

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

    public async Task<ProjectDto> HandleAsync(
        CreateProjectCommand command, CancellationToken ct = default)
    {
        var project = Project.Create(command.Name, command.Description);

        await _repository.AddAsync(project, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return MapToDto(project);
    }

    internal static ProjectDto MapToDto(Project project) => new(
        project.Id,
        project.Name,
        project.Description,
        project.ApiKey,
        project.IsActive,
        project.CreatedAt);
}
