namespace DocumentStorage.Application.Interfaces;

/// <summary>
/// Hashes and verifies passwords. Implementations must use a slow,
/// salted KDF (PBKDF2/Argon2/scrypt).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
