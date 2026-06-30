using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Commands;

public class CompleteUploadCommandHandler
    : ICommandHandler<CompleteUploadCommand, FileDto>
{
    private readonly IStorageProvider _storageProvider;
    private readonly IFileDocumentRepository _repository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly StorageOptions _options;

    public CompleteUploadCommandHandler(
        IStorageProvider storageProvider,
        IFileDocumentRepository repository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        StorageOptions options)
    {
        _storageProvider = storageProvider;
        _repository = repository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public async Task<Result<FileDto>> HandleAsync(
        CompleteUploadCommand command, CancellationToken ct = default)
    {
        string extension;
        try
        {
            extension = FileValidator.ValidateAndExtractExtension(
                command.Name, command.ContentType, command.Size, _options);
        }
        catch (ArgumentException ex)
        {
            return Result<FileDto>.Failure(
                AppError.Validation("INVALID_FILE_SIZE", ex.Message));
        }
        catch (Domain.Exceptions.InvalidFileTypeException ex)
        {
            return Result<FileDto>.Failure(
                AppError.Validation("INVALID_FILE_TYPE", ex.Message));
        }

        var project = await _projectRepository.GetByIdAsync(command.ProjectId, ct).ConfigureAwait(false);
        if (project is null)
            return Result<FileDto>.Failure(
                AppError.NotFound("PROJECT_NOT_FOUND", $"Project '{command.ProjectId}' was not found."));

        var storageKey = StorageKeyGenerator.Generate(project.FolderName, command.Name);

        if (!await _storageProvider.ExistsAsync(storageKey, ct).ConfigureAwait(false))
            return Result<FileDto>.Failure(
                AppError.NotFound("FILE_NOT_FOUND", $"File with id '{command.FileId}' was not found in storage."));

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

        return Result<FileDto>.Success(FileMapper.ToDto(document, downloadUrl));
    }
}
