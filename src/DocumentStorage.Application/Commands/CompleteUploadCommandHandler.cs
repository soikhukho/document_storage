using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Exceptions;

using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

namespace DocumentStorage.Application.Commands;

public class CompleteUploadCommandHandler
    : ICommandHandler<CompleteUploadCommand, FileDto>
{
    private readonly IStorageProvider _storageProvider;
    private readonly IFileDocumentRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StorageOptions _options;

    public CompleteUploadCommandHandler(
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
        CompleteUploadCommand command, CancellationToken ct = default)
    {
        var extension = FileValidator.ValidateAndExtractExtension(
            command.Name, command.ContentType, command.Size, _options);

        var storageKey = StorageKeyGenerator.Generate(command.ProjectId, command.UserId, command.FileId, extension);

        if (!await _storageProvider.ExistsAsync(storageKey, ct).ConfigureAwait(false))
            throw new FileNotFoundException(command.FileId);

        var document = FileDocument.Create(
            command.ProjectId,
            command.Name,
            extension,
            command.ContentType,
            command.Size,
            storageKey,
            _options.Provider,
            command.UserId,
            command.Description);

        await _repository.AddAsync(document, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var downloadUrl = await _storageProvider.GetDownloadUrlAsync(
            storageKey, _options.DownloadExpirationMinutes, ct).ConfigureAwait(false);

        return FileMapper.ToDto(document, downloadUrl);
    }
}
