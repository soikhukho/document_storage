using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Commands;

public class UpdateDescriptionCommandHandler
    : ICommandHandler<UpdateDescriptionCommand, FileDto>
{
    private readonly IStorageProvider _storageProvider;
    private readonly IFileDocumentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StorageOptions _options;

    public UpdateDescriptionCommandHandler(
        IStorageProvider storageProvider,
        IFileDocumentRepository repository,
        IUnitOfWork unitOfWork,
        StorageOptions options)
    {
        _storageProvider = storageProvider;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<Result<FileDto>> HandleAsync(
        UpdateDescriptionCommand command, CancellationToken ct = default)
    {
        var document = command.UserId.HasValue
            ? await _repository.GetByIdAndUserAsync(
                command.FileId, command.ProjectId, command.UserId.Value, ct).ConfigureAwait(false)
            : await _repository.GetByIdAsync(
                command.FileId, command.ProjectId, ct).ConfigureAwait(false);

        if (document is null)
            return Result<FileDto>.Failure(
                AppError.NotFound("FILE_NOT_FOUND", $"File with id '{command.FileId}' was not found."));

        document.UpdateDescription(command.Description);
        await _repository.UpdateAsync(document, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var downloadUrl = await _storageProvider.GetDownloadUrlAsync(
            document.StorageKey, _options.DownloadExpirationMinutes, ct).ConfigureAwait(false);

        return Result<FileDto>.Success(FileMapper.ToDto(document, downloadUrl));
    }
}
