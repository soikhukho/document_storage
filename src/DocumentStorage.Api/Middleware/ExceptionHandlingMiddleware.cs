using System.Diagnostics;
using System.Text.Json;
using DocumentStorage.Domain.Exceptions;
using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

namespace DocumentStorage.Api.Middleware;

/// <summary>
/// Global exception handler — maps domain exceptions to RFC 7807 Problem Details.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title, exposeMessage) = ex switch
        {
            FileNotFoundException       => (404, "File Not Found", true),
            ProjectNotFoundException    => (404, "Project Not Found", true),
            InvalidFileTypeException    => (400, "Invalid File Type", true),
            UploadExpiredException      => (410, "Upload Expired", true),
            PermissionDeniedException   => (403, "Permission Denied", true),
            StorageException            => (502, "Storage Error", true),
            UnauthorizedAccessException  => (401, "Unauthorized", true),
            ArgumentException           => (400, "Validation Error", false),
            _                           => (500, "Internal Server Error", false)
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            _logger.LogWarning("Handled {Type}: {Message}", ex.GetType().Name, ex.Message);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var body = new
        {
            type = $"https://httpstatuses.io/{status}",
            title,
            status,
            detail = exposeMessage ? ex.Message : title,
            instance = context.Request.Path.Value,
            traceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
