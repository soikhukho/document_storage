using DocumentStorage.Application;
using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DocumentStorage.Api.Middleware;

/// <summary>
/// Resolves the caller from the <c>X-API-Key</c> header.
/// If the key matches the configured AdminKey → sets admin flag.
/// Otherwise looks up the project by API key (cached for 5 minutes).
/// Results are stored in <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class ProjectResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _adminKey;

    public ProjectResolutionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _adminKey = configuration["Auth:AdminKey"] ?? string.Empty;
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

            // Admin key check — string comparison, no cache needed
            if (!string.IsNullOrEmpty(_adminKey)
                && string.Equals(key, _adminKey, StringComparison.Ordinal))
            {
                context.Items[HttpContextItemsKeys.IsAdmin] = true;
            }
            else
            {
                var projectId = await cache.GetOrCreateAsync(key, async ct =>
                {
                    var project = await projectRepository.GetByApiKeyAsync(key, ct);
                    return project is not null && project.IsActive ? project.Id : Guid.Empty;
                }, context.RequestAborted);

                if (projectId != Guid.Empty)
                    context.Items[HttpContextItemsKeys.ProjectId] = projectId;
            }
        }

        await _next(context);
    }
}
