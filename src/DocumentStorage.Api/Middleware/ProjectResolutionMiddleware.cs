using DocumentStorage.Application;
using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocumentStorage.Api.Middleware;

/// <summary>
/// Resolves the project (tenant) from the <c>X-API-Key</c> header.
/// Looks up the project by API key (cached for 5 minutes) and stores
/// the ProjectId in <see cref="HttpContext.Items"/>.
/// Admin access is handled exclusively by JWT (<c>Authorization: Bearer</c>).
/// </summary>
public sealed class ProjectResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public ProjectResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IProjectRepository projectRepository,
        IProjectCache cache)
    {
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey)
            && !string.IsNullOrWhiteSpace(apiKey))
        {
            var key = apiKey.ToString();

            var projectId = await cache.GetOrCreateAsync(key, async ct =>
            {
                var project = await projectRepository.GetByApiKeyAsync(key, ct);
                return project is not null && project.IsActive ? project.Id : Guid.Empty;
            }, context.RequestAborted);

            if (projectId != Guid.Empty)
                context.Items[HttpContextItemsKeys.ProjectId] = projectId;
        }

        await _next(context);
    }
}
