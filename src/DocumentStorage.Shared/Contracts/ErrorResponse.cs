namespace DocumentStorage.Shared.Contracts;

/// <summary>
/// Error detail in an API response (RFC 7807 inspired).
/// </summary>
public class ErrorResponse
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public string? Detail { get; set; }
    public string? Target { get; set; }
}
