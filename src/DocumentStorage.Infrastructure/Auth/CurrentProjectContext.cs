using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocumentStorage.Infrastructure.Auth;

/// <summary>
/// Resolves the current project from <see cref="HttpContext.Items"/>,
/// populated by <c>ProjectResolutionMiddleware</c>.
/// </summary>
public class CurrentProjectContext : ICurrentProjectContext
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentProjectContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid ProjectId
    {
        get
        {
            if (_accessor.HttpContext?.Items.TryGetValue("ProjectId", out var val) == true
                && val is Guid id)
                return id;

            return Guid.Empty;
        }
    }

    public bool IsAvailable => ProjectId != Guid.Empty;
}
