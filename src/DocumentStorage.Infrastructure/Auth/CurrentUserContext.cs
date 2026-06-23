using System.Security.Claims;
using DocumentStorage.Application;
using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocumentStorage.Infrastructure.Auth;

/// <summary>
/// Resolves the current user from headers.
/// In production, replace with JWT claims or proper auth middleware.
/// </summary>
public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid UserId
    {
        get
        {
            var httpContext = _accessor.HttpContext;
            if (httpContext is null)
                return Guid.Empty;

            if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var header)
                && Guid.TryParse(header, out var id))
                return id;

            return Guid.Empty;
        }
    }

    public bool IsAuthenticated => UserId != Guid.Empty;

    public bool IsAdmin =>
        _accessor.HttpContext?.Items.TryGetValue(HttpContextItemsKeys.IsAdmin, out var val) == true
        && val is true;

    /// <summary>
    /// Resolved admin user id (set by JWT middleware) or Guid.Empty when
    /// the caller is a project-scoped API key or anonymous.
    /// </summary>
    public Guid AdminUserId
    {
        get
        {
            if (_accessor.HttpContext?.Items.TryGetValue(HttpContextItemsKeys.AdminUserId, out var val) == true
                && val is Guid id)
                return id;
            return Guid.Empty;
        }
    }

    public string? AdminUsername
        => _accessor.HttpContext?.Items.TryGetValue(HttpContextItemsKeys.AdminUsername, out var val) == true
            ? val as string
            : null;
}
