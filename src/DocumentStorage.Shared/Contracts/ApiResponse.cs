namespace DocumentStorage.Shared.Contracts;

/// <summary>
/// Standard API response envelope for all endpoints.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public ErrorResponse[] Errors { get; init; } = Array.Empty<ErrorResponse>();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T? data = default, string? message = null)
        => new()
        {
            Success = true,
            Data = data,
            Message = message ?? "Request processed successfully.",
            Errors = Array.Empty<ErrorResponse>(),
            Timestamp = DateTimeOffset.UtcNow
        };

    public static ApiResponse<T> Fail(string message, params ErrorResponse[] errors)
        => new()
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors ?? Array.Empty<ErrorResponse>(),
            Timestamp = DateTimeOffset.UtcNow
        };
}

/// <summary>
/// Non-generic ApiResponse for responses without data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null)
        => new()
        {
            Success = true,
            Data = null,
            Message = message ?? "Request processed successfully.",
            Errors = Array.Empty<ErrorResponse>(),
            Timestamp = DateTimeOffset.UtcNow
        };

    public new static ApiResponse Fail(string message, params ErrorResponse[] errors)
        => new()
        {
            Success = false,
            Data = null,
            Message = message,
            Errors = errors ?? Array.Empty<ErrorResponse>(),
            Timestamp = DateTimeOffset.UtcNow
        };
}
