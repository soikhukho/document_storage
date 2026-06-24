using System.Diagnostics;
using System.Text.Json;
using DocumentStorage.Domain.Exceptions;
using DocumentStorage.Shared.Contracts;
using FileNotFoundException = DocumentStorage.Domain.Exceptions.FileNotFoundException;

namespace DocumentStorage.Api.Middleware;

/// <summary>
/// Global exception handler — maps unhandled exceptions to standardized ApiResponse error format.
/// Acts as a safety net for exceptions not caught by the Result pattern.
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
        var (status, code, message) = ex switch
        {
            FileNotFoundException       => (404, "FILE_NOT_FOUND", ex.Message),
            ProjectNotFoundException    => (404, "PROJECT_NOT_FOUND", ex.Message),
            InvalidFileTypeException    => (400, "INVALID_FILE_TYPE", ex.Message),
            UploadExpiredException      => (410, "UPLOAD_EXPIRED", ex.Message),
            PermissionDeniedException   => (403, "PERMISSION_DENIED", ex.Message),
            InvalidCredentialsException => (401, "INVALID_CREDENTIALS", ex.Message),
            StorageException            => (502, "STORAGE_ERROR", ex.Message),
            UnauthorizedAccessException  => (401, "UNAUTHORIZED", ex.Message),
            ArgumentException           => (400, "VALIDATION_ERROR", ex.Message),
            _                           => (500, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            _logger.LogWarning("Handled {Type}: {Message}", ex.GetType().Name, ex.Message);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.Fail(message, new ErrorResponse
        {
            Code = code,
            Message = message
        });

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
