namespace DocumentStorage.Domain.Exceptions;

/// <summary>
/// Thrown when login credentials are invalid or the user is not found.
/// Uses the same message for both cases to avoid username enumeration.
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("Invalid username or password.")
    {
    }
}
