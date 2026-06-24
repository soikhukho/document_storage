namespace DocumentStorage.Shared.Results;

/// <summary>
/// Determines HTTP status code mapping and error handling behavior.
/// </summary>
public enum ErrorType
{
    Validation = 0,
    NotFound = 1,
    Conflict = 2,
    Unauthorized = 3,
    Forbidden = 4,
    Failure = 5
}
