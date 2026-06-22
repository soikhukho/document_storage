using DocumentStorage.Application.Common;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;

namespace DocumentStorage.Application.ProjectCommands;

public class SetProjectActiveCommandHandler
    : ICommandHandler<SetProjectActiveCommand>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProjectCache _cache;

    public SetProjectActiveCommandHandler(
        IProjectRepository repository,
        IUnitOfWork unitOfWork,
        IProjectCache cache)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task HandleAsync(
        SetProjectActiveCommand command, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(command.ProjectId, ct).ConfigureAwait(false)
            ?? throw new ProjectNotFoundException(command.ProjectId);

        if (command.IsActive)
            project.Activate();
        else
            project.Deactivate();

        await _repository.UpdateAsync(project, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _cache.Invalidate(project.ApiKey);
    }
}
