using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Commands;

public class PurgeFileCommandHandler
    : ICommandHandler<PurgeFileCommand>
{
    private readonly IStorageProvider _storageProvider;
    private readonly IFileDocumentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PurgeFileCommandHandler(
        IStorageProvider storageProvider,
        IFileDocumentRepository repository,
        IUnitOfWork unitOfWork)
    {
        _storageProvider = storageProvider;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(PurgeFileCommand command, CancellationToken ct = default)
    {
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

        // Only delete the S3 object if no other DB row (active or in trash) still
        // references the same storage key. This prevents accidental data loss when
        // two records share a storage key (e.g., a new upload reused a name that
        // was already in the trash).
        var otherRefCount = await _repository
            .CountOtherReferencesByStorageKeyAsync(document.StorageKey, document.Id, ct)
            .ConfigureAwait(false);

        if (otherRefCount == 0)
        {
            await _storageProvider.DeleteAsync(document.StorageKey, ct).ConfigureAwait(false);
        }

        // Permanent DB removal (bypasses soft-delete).
        await _repository.HardRemoveAsync(document, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
