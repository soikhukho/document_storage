using DocumentStorage.Application;
using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocumentStorage.Api.Middleware;

/// <summary>
/// Parses the <c>Authorization: Bearer &lt;jwt&gt;</c> header and, when the
/// token is a valid admin JWT, populates <see cref="HttpContextItemsKeys.IsAdmin"/>
/// and <see cref="HttpContextItemsKeys.AdminUserId"/> for downstream consumers.
///
/// Anonymous requests (no Bearer token) are passed through; per-endpoint
/// authorization (<c>[AdminOnly]</c>) decides whether to reject them.
/// </summary>
public sealed class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IJwtTokenService jwtTokenService)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (!string.IsNullOrWhiteSpace(token))
            {
                var adminId = await jwtTokenService.ValidateTokenAsync(token);
                if (adminId is Guid id && id != Guid.Empty)
                {
                    context.Items[HttpContextItemsKeys.IsAdmin] = true;
                    context.Items[HttpContextItemsKeys.AdminUserId] = id;

                    // Username is encoded in the JWT "unique_name" claim.
                    if (context.User?.Identity?.Name is { } name)
                        context.Items[HttpContextItemsKeys.AdminUsername] = name;
                }
            }
        }

        await _next(context);
    }
}
