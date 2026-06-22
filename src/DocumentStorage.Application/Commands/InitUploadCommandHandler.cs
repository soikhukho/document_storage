using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;

namespace DocumentStorage.Application.Commands;

public class InitUploadCommandHandler
    : ICommandHandler<InitUploadCommand, InitUploadResponse>
{
    private readonly IStorageProvider _storageProvider;
    private readonly StorageOptions _options;

    public InitUploadCommandHandler(IStorageProvider storageProvider, StorageOptions options)
    {
        _storageProvider = storageProvider;
        _options = options;
    }

    public async Task<InitUploadResponse> HandleAsync(
        InitUploadCommand command, CancellationToken ct = default)
    {
        var extension = FileValidator.ValidateAndExtractExtension(
            command.Name, command.ContentType, command.Size, _options);

        var fileId = Guid.NewGuid();
        var storageKey = StorageKeyGenerator.Generate(command.ProjectId, command.UserId, fileId, extension);

        var instruction = await _storageProvider.InitUploadAsync(
            storageKey, command.ContentType, command.Size,
            _options.UploadExpirationMinutes, ct).ConfigureAwait(false);

        return new InitUploadResponse(
            fileId,
            instruction.UploadUrl,
            instruction.Headers,
            instruction.ExpiredAt);
    }
}
