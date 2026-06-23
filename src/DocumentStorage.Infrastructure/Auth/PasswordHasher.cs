using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace DocumentStorage.Infrastructure.Auth;

/// <summary>
/// Wraps <see cref="PasswordHasher{T}"/> from ASP.NET Core Identity,
/// which uses PBKDF2 with HMAC-SHA256 and a random salt.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string password)
        => _inner.HashPassword(new object(), password);

    public bool Verify(string password, string hash)
        => _inner.VerifyHashedPassword(new object(), hash, password)
           == PasswordVerificationResult.Success;
}
