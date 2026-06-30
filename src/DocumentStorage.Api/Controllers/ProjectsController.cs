using System.ComponentModel.DataAnnotations;
using DocumentStorage.Api.Attributes;
using DocumentStorage.Api.Extensions;
using DocumentStorage.Api.Models;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.ProjectCommands;
using DocumentStorage.Application.ProjectQueries;
using DocumentStorage.Application.Queries;
using DocumentStorage.Shared.Contracts;
using DocumentStorage.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace DocumentStorage.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;
    private readonly ICurrentProjectContext _currentProject;

    public ProjectsController(
        ICurrentUserContext currentUser,
        ICurrentProjectContext currentProject)
    {
        _currentUser = currentUser;
        _currentProject = currentProject;
    }

    private bool IsAdmin => _currentUser.IsAdmin;

    // ── Admin-only endpoints ──

    /// <summary>
    /// Create a new project. Returns project info with API key. [Admin]
    /// </summary>
    [HttpPost]
    [AdminOnly]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        [FromServices] ICommandHandler<CreateProjectCommand, ProjectDto> handler,
        CancellationToken ct)
    {
        var command = new CreateProjectCommand(request.Name, request.FolderName, request.Description);
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result, successStatus: 201);
    }

    /// <summary>
    /// List all projects (paged). [Admin]
    /// </summary>
    [HttpGet]
    [AdminOnly]
    public async Task<IActionResult> GetAll(
        [FromServices] IQueryHandler<GetAllProjectsQuery, PagedResult<ProjectDto>> handler,
        CancellationToken ct,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var query = new GetAllProjectsQuery(page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Update project name and/or description. [Admin]
    /// </summary>
    [HttpPut("{id:guid}")]
    [AdminOnly]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        [FromServices] ICommandHandler<UpdateProjectCommand, ProjectDto> handler,
        CancellationToken ct)
    {
        var command = new UpdateProjectCommand(id, request.Name, request.Description);
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result);
    }

    /// <summary>
    /// Activate or deactivate a project. [Admin]
    /// </summary>
    [HttpPatch("{id:guid}/active")]
    [AdminOnly]
    public async Task<IActionResult> SetActive(
        Guid id,
        [FromBody] bool isActive,
        [FromServices] ICommandHandler<SetProjectActiveCommand> handler,
        CancellationToken ct)
    {
        var command = new SetProjectActiveCommand(id, isActive);
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result, successStatus: 204);
    }

    /// <summary>
    /// Regenerate the API key for a project. [Admin]
    /// </summary>
    [HttpPost("{id:guid}/regenerate-key")]
    [AdminOnly]
    public async Task<IActionResult> RegenerateApiKey(
        Guid id,
        [FromServices] ICommandHandler<RegenerateApiKeyCommand, ProjectDto> handler,
        CancellationToken ct)
    {
        var command = new RegenerateApiKeyCommand(id);
        var result = await handler.HandleAsync(command, ct);
        return this.ToActionResult(result);
    }

    // ── Mixed access endpoints ──

    /// <summary>
    /// Get a project by id.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetProjectByIdQuery, ProjectDto> handler,
        CancellationToken ct)
    {
        if (!IsAdmin && id != _currentProject.ProjectId)
            return StatusCode(403, ApiResponse.Fail("You can only access your own project."));

        var query = new GetProjectByIdQuery(id);
        var result = await handler.HandleAsync(query, ct);

        if (result.IsSuccess && !IsAdmin)
            return this.ToActionResult(Result<ProjectDto>.Success(result.Value! with { ApiKey = "" }));

        return this.ToActionResult(result);
    }

    /// <summary>
    /// List files in a specific project.
    /// </summary>
    [HttpGet("{id:guid}/files")]
    public async Task<IActionResult> GetProjectFiles(
        Guid id,
        [FromServices] IQueryHandler<SearchFilesQuery, PagedResult<FileDto>> handler,
        CancellationToken ct,
        [FromQuery] string? keyword = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc")
    {
        if (!IsAdmin && id != _currentProject.ProjectId)
            return StatusCode(403, ApiResponse.Fail("You can only access your own project."));

        var query = new SearchFilesQuery(
            id, keyword, null, page, pageSize, sortBy, sortDirection);

        var result = await handler.HandleAsync(query, ct);
        return this.ToActionResult(result);
    }
}
