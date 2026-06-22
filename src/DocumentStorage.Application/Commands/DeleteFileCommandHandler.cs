using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;

using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

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

    public async Task HandleAsync(DeleteFileCommand command, CancellationToken ct = default)
    {
        var document = command.UserId.HasValue
            ? await _repository.GetByIdAndUserAsync(
                command.FileId, command.ProjectId, command.UserId.Value, ct)
            : await _repository.GetByIdAsync(
                command.FileId, command.ProjectId, ct);

        if (document is null)
            throw new FileNotFoundException(command.FileId);

        document.SoftDelete();
        await _repository.UpdateAsync(document, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _storageProvider.DeleteAsync(document.StorageKey, ct);
    }
}
