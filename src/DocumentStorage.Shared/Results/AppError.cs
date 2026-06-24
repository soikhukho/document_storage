namespace DocumentStorage.Shared.Results;

/// <summary>
/// Structured application error with code, message, type, and optional detail.
/// </summary>
public sealed class AppError
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public string? Detail { get; }

    public AppError(string code, string message, ErrorType type = ErrorType.Failure, string? detail = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Type = type;
        Detail = detail;
    }

    public static AppError Validation(string code, string message, string? fieldName = null)
        => new(code, message, ErrorType.Validation, fieldName);

    public static AppError NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    public static AppError Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    public static AppError Unauthorized(string code, string message)
        => new(code, message, ErrorType.Unauthorized);

    public static AppError Forbidden(string code, string message)
        => new(code, message, ErrorType.Forbidden);

    public static AppError Failure(string code, string message)
        => new(code, message, ErrorType.Failure);

    public override string ToString()
        => $"{Code}: {Message}" + (Detail is not null ? $" [{Detail}]" : string.Empty);
}
