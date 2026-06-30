using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Commands;

public class InitUploadCommandHandler
    : ICommandHandler<InitUploadCommand, InitUploadResponse>
{
    private readonly IStorageProvider _storageProvider;
    private readonly IFileDocumentRepository _fileRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly StorageOptions _options;

    public InitUploadCommandHandler(
        IStorageProvider storageProvider,
        IFileDocumentRepository fileRepository,
        IProjectRepository projectRepository,
        StorageOptions options)
    {
        _storageProvider = storageProvider;
        _fileRepository = fileRepository;
        _projectRepository = projectRepository;
        _options = options;
    }

    public async Task<Result<InitUploadResponse>> HandleAsync(
        InitUploadCommand command, CancellationToken ct = default)
    {
        string extension;
        try
        {
            extension = FileValidator.ValidateAndExtractExtension(
                command.Name, command.ContentType, command.Size, _options);
        }
        catch (ArgumentException ex)
        {
            return Result<InitUploadResponse>.Failure(
                AppError.Validation("INVALID_FILE_SIZE", ex.Message));
        }
        catch (Domain.Exceptions.InvalidFileTypeException ex)
        {
            return Result<InitUploadResponse>.Failure(
                AppError.Validation("INVALID_FILE_TYPE", ex.Message));
        }

        // Block duplicate filename in the same project BEFORE issuing a presigned URL.
        if (await _fileRepository.ExistsByNameAsync(command.ProjectId, command.Name, ct).ConfigureAwait(false))
        {
            return Result<InitUploadResponse>.Failure(
                AppError.Conflict(
                    "FILE_NAME_EXISTS",
                    $"A file named '{command.Name}' already exists in this project."));
        }

        var project = await _projectRepository.GetByIdAsync(command.ProjectId, ct).ConfigureAwait(false);
        if (project is null)
            return Result<InitUploadResponse>.Failure(
                AppError.NotFound("PROJECT_NOT_FOUND", $"Project '{command.ProjectId}' was not found."));

        var storageKey = StorageKeyGenerator.Generate(project.FolderName, command.Name);

        var instruction = await _storageProvider.InitUploadAsync(
            storageKey, command.ContentType, command.Size,
            _options.UploadExpirationMinutes, ct).ConfigureAwait(false);

        return Result<InitUploadResponse>.Success(new InitUploadResponse(
            Guid.NewGuid(),
            instruction.UploadUrl,
            instruction.Headers,
            instruction.ExpiredAt));
    }
}
