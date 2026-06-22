using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;

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

    public async Task<ProjectDto> HandleAsync(
        UpdateProjectCommand command, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, ct)
            ?? throw new ProjectNotFoundException(command.ProjectId);

        project.Update(command.Name, command.Description);
        await _repository.UpdateAsync(project, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return CreateProjectCommandHandler.MapToDto(project);
    }
}
