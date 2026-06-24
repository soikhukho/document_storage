using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Commands;

public class DeleteFileCommandHandler
    : ICommandHandler<DeleteFileCommand>
{
    private readonly IStorageProvider _storageProvider;
    private readonly IFileDocumentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteFileCommandHandler(
        IStorageProvider storageProvider,
        IFileDocumentRepository repository,
        IUnitOfWork unitOfWork)
    {
        _storageProvider = storageProvider;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(DeleteFileCommand command, CancellationToken ct = default)
    {
        var document = command.UserId.HasValue
            ? await _repository.GetByIdAndUserAsync(
                command.FileId, command.ProjectId, command.UserId.Value, ct).ConfigureAwait(false)
            : await _repository.GetByIdAsync(
                command.FileId, command.ProjectId, ct).ConfigureAwait(false);

        if (document is null)
            return Result.Failure(
                AppError.NotFound("FILE_NOT_FOUND", $"File with id '{command.FileId}' was not found."));

        document.SoftDelete();
        await _repository.UpdateAsync(document, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        await _storageProvider.DeleteAsync(document.StorageKey, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
