using System.ComponentModel.DataAnnotations;
using DocumentStorage.Api.Models;
using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.Queries;
using DocumentStorage.Infrastructure.Storage;
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

    // Admin sees all projects; regular user is scoped to their API-key project
    private Guid? GetScopedProjectId()
    {
        if (_currentUser.IsAdmin)
            return null;

        if (!_currentProject.IsAvailable)
            throw new UnauthorizedAccessException("Valid X-API-Key header is required.");

        return _currentProject.ProjectId;
    }

    // Resolves a required projectId for single-file endpoints.
    // Admin must pass ?projectId=...; non-admin is scoped to their API-key project.
    private Guid ResolveProjectId(Guid? projectIdParam)
    {
        if (_currentUser.IsAdmin)
        {
            if (projectIdParam is { } id && id != Guid.Empty)
                return id;

            throw new UnauthorizedAccessException("Admin must specify a project via the 'projectId' query parameter.");
        }

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
    public async Task<ActionResult<InitUploadResponse>> InitUpload(
        [FromBody] InitUploadRequest request,
        [FromServices] ICommandHandler<InitUploadCommand, InitUploadResponse> handler,
        CancellationToken ct)
    {
        var projectId = GetScopedProjectId()
            ?? throw new UnauthorizedAccessException("Admin cannot upload files without a project API key.");

        var command = new InitUploadCommand(
            projectId, request.Name, request.ContentType, request.Size, GetUserId());

        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Phase 2 of two-phase upload — verify upload and persist metadata.
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<FileDto>> CompleteUpload(
        [FromBody] CompleteUploadRequest request,
        [FromServices] ICommandHandler<CompleteUploadCommand, FileDto> handler,
        CancellationToken ct)
    {
        var projectId = GetScopedProjectId()
            ?? throw new UnauthorizedAccessException("Admin cannot upload files without a project API key.");

        var command = new CompleteUploadCommand(
            projectId, request.FileId, request.Name, request.ContentType, request.Size,
            GetUserId(), request.Description);

        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a single file by id (generates a fresh presigned download URL).
    /// Admin: any project via ?projectId=. User: only their own project.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FileDto>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetFileByIdQuery, FileDto> handler,
        CancellationToken ct,
        [FromQuery] Guid? projectId = null)
    {
        var pid = ResolveProjectId(projectId);
        Guid? userId = _currentUser.IsAdmin ? null : GetUserId();

        var query = new GetFileByIdQuery(pid, id, userId);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Soft-delete a file and remove it from storage.
    /// Admin: any project via ?projectId=. User: only their own project.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteFileCommand> handler,
        CancellationToken ct,
        [FromQuery] Guid? projectId = null)
    {
        var pid = ResolveProjectId(projectId);
        Guid? userId = _currentUser.IsAdmin ? null : GetUserId();

        var command = new DeleteFileCommand(pid, id, userId);
        await handler.HandleAsync(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Search files with keyword, pagination, and sorting.
    /// Admin: returns files across all projects. User: only their project.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<FileDto>>> Search(
        [FromServices] IQueryHandler<SearchFilesQuery, PagedResult<FileDto>> handler,
        CancellationToken ct,
        [FromQuery] string? keyword = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc")
    {
        var query = new SearchFilesQuery(
            GetScopedProjectId(), keyword, null, page, pageSize, sortBy, sortDirection);

        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// List all files belonging to a specific user.
    /// Admin: across all projects. User: only their project.
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<PagedResult<FileDto>>> GetByUser(
        Guid userId,
        [FromServices] IQueryHandler<GetFilesByUserQuery, PagedResult<FileDto>> handler,
        CancellationToken ct,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var query = new GetFilesByUserQuery(GetScopedProjectId(), userId, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Update the description on an existing file.
    /// Admin: any project via ?projectId=. User: only their own project.
    /// </summary>
    [HttpPatch("{id:guid}/description")]
    public async Task<ActionResult<FileDto>> UpdateDescription(
        Guid id,
        [FromBody] UpdateDescriptionRequest request,
        [FromServices] ICommandHandler<UpdateDescriptionCommand, FileDto> handler,
        CancellationToken ct,
        [FromQuery] Guid? projectId = null)
    {
        var pid = ResolveProjectId(projectId);
        Guid? userId = _currentUser.IsAdmin ? null : GetUserId();

        var command = new UpdateDescriptionCommand(pid, id, userId, request.Description);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
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
        if (!_currentUser.IsAdmin && !_currentProject.IsAvailable)
            return Unauthorized(new { title = "Unauthorized", detail = "Valid X-API-Key header is required." });

        if (storageProvider is not LocalStorageProvider local)
            return BadRequest("Local storage provider is not active.");

        await local.WriteAsync(storageKey, Request.Body, ct);
        return Ok();
    }

    [HttpGet("local-download/{*storageKey}")]
    public async Task<IActionResult> LocalDownload(
        string storageKey,
        [FromServices] IStorageProvider storageProvider,
        CancellationToken ct)
    {
        if (!_currentUser.IsAdmin && !_currentProject.IsAvailable)
            return Unauthorized(new { title = "Unauthorized", detail = "Valid X-API-Key header is required." });

        if (storageProvider is not LocalStorageProvider local)
            return BadRequest("Local storage provider is not active.");

        var stream = await local.OpenReadAsync(storageKey, ct);
        return File(stream, "application/octet-stream");
    }
}
