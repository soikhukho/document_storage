using DocumentStorage.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DocumentStorage.Api.Attributes;

/// <summary>
/// Requires the caller to be an admin (X-API-Key matches AdminKey).
/// Returns 403 Forbidden otherwise.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isAdmin = context.HttpContext.Items.TryGetValue(HttpContextItemsKeys.IsAdmin, out var val) && val is true;

        if (!isAdmin)
        {
            context.Result = new ObjectResult(new { title = "Forbidden", detail = "Admin access required." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
