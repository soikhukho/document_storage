using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

namespace DocumentStorage.Application.Queries;

public class GetFileByIdQueryHandler
    : IQueryHandler<GetFileByIdQuery, FileDto>
{
    private readonly IFileDocumentRepository _repository;
    private readonly IStorageProvider _storageProvider;
    private readonly StorageOptions _options;

    public GetFileByIdQueryHandler(
        IFileDocumentRepository repository,
        IStorageProvider storageProvider,
        StorageOptions options)
    {
        _repository = repository;
        _storageProvider = storageProvider;
        _options = options;
    }

    public async Task<Result<FileDto>> HandleAsync(GetFileByIdQuery query, CancellationToken ct = default)
    {
        var document = query.UserId.HasValue
            ? await _repository.GetByIdAndUserAsync(query.FileId, query.ProjectId, query.UserId.Value, ct).ConfigureAwait(false)
            : await _repository.GetByIdAsync(query.FileId, query.ProjectId, ct).ConfigureAwait(false);

        if (document is null)
            return Result<FileDto>.Failure(
                AppError.NotFound("FILE_NOT_FOUND", $"File with id '{query.FileId}' was not found."));

        var downloadUrl = await _storageProvider.GetDownloadUrlAsync(
            document.StorageKey, _options.DownloadExpirationMinutes, ct).ConfigureAwait(false);

        return Result<FileDto>.Success(FileMapper.ToDto(document, downloadUrl));
    }
}
