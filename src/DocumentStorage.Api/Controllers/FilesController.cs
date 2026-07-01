using System.ComponentModel.DataAnnotations;
using DocumentStorage.Api.Extensions;
using DocumentStorage.Api.Models;
using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.Queries;
using DocumentStorage.Infrastructure.Storage;
using DocumentStorage.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace DocumentStorage.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;
    private readonly ICurrentProjectContext _currentProject;

    public FilesController(
        ICurrentUserContext currentUser,
        ICurrentProjectContext currentProject)
    {
        _currentUser = currentUser;
        _currentProject = currentProject;
    }

    private Guid GetUserId()
    {
        if (_currentUser.UserId != Guid.Empty)
            return _currentUser.UserId;

        if (Request.Headers.TryGetValue("X-User-Id", out var header)
            && Guid.TryParse(header, out var id))
            return id;

        return Guid.Empty;
    }

    private Guid RequireProjectId()
    {
        if (!_currentProject.IsAvailable)
            throw new UnauthorizedAccessException("Valid X-API-Key header is required.");

        return _currentProject.ProjectId;
    }

    // ──────────────────────────────────────────────────────────────
    //  SDD §15 API Contract
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 1 of two-phase upload — request a presigned upload URL.
    /// </summary>
    [HttpPost("init-upload")]
    public async Task<IActionResult> InitUpload(
        [FromBody] InitUploadRequest request,
        [FromServices] ICommandHandler<InitUploadCommand, InitUploadResponse> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var command = new InitUploadCommand(
            projectId, request.Name, request.ContentType, request.Size, GetUserId());

        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Phase 2 of two-phase upload — verify upload and persist metadata.
    /// </summary>
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteUpload(
        [FromBody] CompleteUploadRequest request,
        [FromServices] ICommandHandler<CompleteUploadCommand, FileDto> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var command = new CompleteUploadCommand(
            projectId, request.FileId, request.Name, request.ContentType, request.Size,
            GetUserId(), request.Description);

        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result, successStatus: 201);
    }

    /// <summary>
    /// Get a single file by id (generates a fresh presigned download URL).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetFileByIdQuery, FileDto> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var query = new GetFileByIdQuery(projectId, id, GetUserId());
        var result = await handler.HandleAsync(query, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Soft-delete a file and remove it from storage.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteFileCommand> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var command = new DeleteFileCommand(projectId, id, GetUserId());
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result, successStatus: 204);
    }

    /// <summary>
    /// List files in the project's trash bin (soft-deleted files).
    /// </summary>
    [HttpGet("trash")]
    public async Task<IActionResult> GetTrash(
        [FromServices] IQueryHandler<GetTrashQuery, PagedResult<FileDto>> handler,
        CancellationToken ct,
        [FromQuery] string? keyword = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc")
    {
        var projectId = RequireProjectId();
        var query = new GetTrashQuery(
            projectId, null, keyword, page, pageSize, sortBy, sortDirection);

        var result = await handler.HandleAsync(query, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Restore a soft-deleted file from the trash bin.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromServices] ICommandHandler<RestoreFileCommand> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var command = new RestoreFileCommand(projectId, id, GetUserId());
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Permanently remove a file from trash: deletes the S3 object and the DB row.
    /// </summary>
    [HttpDelete("{id:guid}/purge")]
    public async Task<IActionResult> Purge(
        Guid id,
        [FromServices] ICommandHandler<PurgeFileCommand> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var command = new PurgeFileCommand(projectId, id, GetUserId());
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result, successStatus: 204);
    }

    /// <summary>
    /// Search files with keyword, pagination, and sorting.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromServices] IQueryHandler<SearchFilesQuery, PagedResult<FileDto>> handler,
        CancellationToken ct,
        [FromQuery] string? keyword = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc")
    {
        var projectId = RequireProjectId();
        var query = new SearchFilesQuery(
            projectId, keyword, null, page, pageSize, sortBy, sortDirection);

        var result = await handler.HandleAsync(query, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// List all files belonging to a specific user.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(
        Guid userId,
        [FromServices] IQueryHandler<GetFilesByUserQuery, PagedResult<FileDto>> handler,
        CancellationToken ct,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var projectId = RequireProjectId();
        var query = new GetFilesByUserQuery(projectId, userId, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update the description on an existing file.
    /// </summary>
    [HttpPatch("{id:guid}/description")]
    public async Task<IActionResult> UpdateDescription(
        Guid id,
        [FromBody] UpdateDescriptionRequest request,
        [FromServices] ICommandHandler<UpdateDescriptionCommand, FileDto> handler,
        CancellationToken ct)
    {
        var projectId = RequireProjectId();
        var command = new UpdateDescriptionCommand(projectId, id, GetUserId(), request.Description);
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result);
    }

    // ──────────────────────────────────────────────────────────────
    //  Local storage provider endpoints (Client → API → Disk)
    // ──────────────────────────────────────────────────────────────

    [HttpPut("local-upload/{*storageKey}")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> LocalUpload(
        string storageKey,
        [FromServices] IStorageProvider storageProvider,
        CancellationToken ct)
    {
        if (!_currentProject.IsAvailable)
            return Unauthorized(ApiResponse.Fail("Valid X-API-Key header is required."));

        if (storageProvider is not LocalStorageProvider local)
            return BadRequest(ApiResponse.Fail("Local storage provider is not active."));

        await local.WriteAsync(storageKey, Request.Body, ct);
        return Ok(ApiResponse.Ok());
    }

    [HttpGet("local-download/{*storageKey}")]
    public async Task<IActionResult> LocalDownload(
        string storageKey,
        [FromServices] IStorageProvider storageProvider,
        CancellationToken ct)
    {
        if (!_currentProject.IsAvailable)
            return Unauthorized(ApiResponse.Fail("Valid X-API-Key header is required."));

        if (storageProvider is not LocalStorageProvider local)
            return BadRequest(ApiResponse.Fail("Local storage provider is not active."));

        var stream = await local.OpenReadAsync(storageKey, ct);
        return File(stream, "application/octet-stream");
    }
}
