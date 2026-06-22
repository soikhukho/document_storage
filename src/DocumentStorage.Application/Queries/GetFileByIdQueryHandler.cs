using DocumentStorage.Application.Common;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;

using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

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

    public async Task<FileDto> HandleAsync(GetFileByIdQuery query, CancellationToken ct = default)
    {
        var document = await _repository.GetByIdAndUserAsync(query.FileId, query.ProjectId, query.UserId, ct)
            ?? throw new FileNotFoundException(query.FileId);

        var downloadUrl = await _storageProvider.GetDownloadUrlAsync(
            document.StorageKey, _options.DownloadExpirationMinutes, ct);

        return FileMapper.ToDto(document, downloadUrl);
    }
}
