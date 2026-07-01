using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocumentStorage.Infrastructure.Auth;

/// <summary>
/// Resolves the current user from the <c>X-User-Id</c> header.
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
}
