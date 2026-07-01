using DocumentStorage.Application;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DocumentStorage.Api.Filters;

/// <summary>
/// Global action filter that records audit entries for all mutating
/// HTTP methods (POST, PUT, PATCH, DELETE).
/// Reads actor information from <see cref="HttpContext.Items"/>
/// populated by the auth middleware pipeline.
/// </summary>
public sealed class AuditLogActionFilter : IAsyncActionFilter
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    private readonly IAuditLogger _auditLogger;

    public AuditLogActionFilter(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var method = http.Request.Method;

        // Non-mutating requests (GET, HEAD, OPTIONS) — skip audit.
        if (!MutatingMethods.Contains(method))
        {
            await next();
            return;
        }

        var executed = await next();

        // Determine success and status code.
        var success = executed.Exception is null || executed.ExceptionHandled;
        var statusCode = GetStatusCode(executed.Result, success);

        // Extract route values for action label.
        var controller = executed.ActionDescriptor.RouteValues.TryGetValue("controller", out var c) ? c : "Unknown";
        var action = executed.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a : "Unknown";

        // Try to extract the route "id" parameter.
        string? entityId = null;
        if (executed.ActionDescriptor.RouteValues.TryGetValue("id", out var routeId)
            && !string.IsNullOrWhiteSpace(routeId))
        {
            entityId = routeId;
        }

        // Resolve actor info from HttpContext.Items.
        var (actorType, actorId, projectId) = ResolveActor(http);

        var entry = new AuditLogEntry(
            HttpMethod: method,
            Path: http.Request.Path.Value ?? "/",
            Action: $"{controller}.{action}",
            StatusCode: statusCode,
            ActorType: actorType,
            ActorId: actorId,
            ProjectId: projectId,
            EntityId: entityId,
            IPAddress: http.Connection.RemoteIpAddress?.ToString(),
            UserAgent: http.Request.Headers.UserAgent.ToString(),
            Details: null);

        await _auditLogger.LogAsync(entry, http.RequestAborted);
    }

    private static int GetStatusCode(Microsoft.AspNetCore.Mvc.IActionResult? result, bool success)
    {
        if (result is ObjectResult obj && obj.StatusCode.HasValue)
            return obj.StatusCode.Value;

        if (result is StatusCodeResult sc)
            return sc.StatusCode;

        return success ? 200 : 500;
    }

    private static (AuditActorType actorType, string? actorId, Guid? projectId) ResolveActor(HttpContext http)
    {
        var isAdmin = http.Items.TryGetValue(HttpContextItemsKeys.IsAdmin, out var adminVal)
                      && adminVal is true;

        if (isAdmin)
        {
            var adminId = http.Items.TryGetValue(HttpContextItemsKeys.AdminUserId, out var idVal)
                          && idVal is Guid g ? g.ToString() : null;
            return (AuditActorType.Admin, adminId, null);
        }

        if (http.Items.TryGetValue(HttpContextItemsKeys.ProjectId, out var pidVal)
            && pidVal is Guid projectId)
        {
            return (AuditActorType.Project, projectId.ToString(), projectId);
        }

        return (AuditActorType.Anonymous, null, null);
    }
}
