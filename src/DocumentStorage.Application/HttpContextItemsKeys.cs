namespace DocumentStorage.Application;

/// <summary>
/// Typed keys for <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/>
/// to avoid magic strings across middleware and auth contexts.
/// </summary>
public static class HttpContextItemsKeys
{
    public const string IsAdmin = "IsAdmin";
    public const string ProjectId = "ProjectId";
}
