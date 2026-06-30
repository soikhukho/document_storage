using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Commands;

public class RestoreFileCommandHandler
    : ICommandHandler<RestoreFileCommand>
{
    private readonly IFileDocumentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreFileCommandHandler(
        IFileDocumentRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(RestoreFileCommand command, CancellationToken ct = default)
    {
        // Must look up via IgnoreQueryFilters since the file is soft-deleted.
        var document = await _repository.GetDeletedByIdAsync(
            command.FileId, command.ProjectId, ct).ConfigureAwait(false);

        if (document is null)
            return Result.Failure(
                AppError.NotFound("FILE_NOT_FOUND",
                    $"File with id '{command.FileId}' is not in the trash of this project."));

        if (command.UserId.HasValue && document.UploadedBy != command.UserId.Value)
            return Result.Failure(
                AppError.NotFound("FILE_NOT_FOUND",
                    $"File with id '{command.FileId}' is not in the trash of this user."));

        // Block restore if a non-deleted file with the same name already exists in the project
        // (would create ambiguity in storage key).
        if (await _repository.ExistsByNameAsync(command.ProjectId, document.Name, ct).ConfigureAwait(false))
            return Result.Failure(
                AppError.Conflict(
                    "FILE_NAME_EXISTS",
                    $"A file named '{document.Name}' already exists in this project. " +
                    "Rename or delete the active file before restoring."));

        document.Restore();
        await _repository.UpdateAsync(document, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
