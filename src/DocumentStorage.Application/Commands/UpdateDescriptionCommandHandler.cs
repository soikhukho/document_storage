using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;

using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

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

    public async Task<FileDto> HandleAsync(
        UpdateDescriptionCommand command, CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAndUserAsync(
            command.FileId, command.ProjectId, command.UserId, ct)
            ?? throw new FileNotFoundException(command.FileId);

        document.UpdateDescription(command.Description);
        await _repository.UpdateAsync(document, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var downloadUrl = await _storageProvider.GetDownloadUrlAsync(
            document.StorageKey, _options.DownloadExpirationMinutes, ct);

        return FileMapper.ToDto(document, downloadUrl);
    }
}
