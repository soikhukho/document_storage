using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Exceptions;

namespace DocumentStorage.Application.AuthCommands;

public class LoginCommandHandler
    : ICommandHandler<LoginCommand, LoginResult>
{
    private readonly IAdminUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IAdminUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResult> HandleAsync(
        LoginCommand command, CancellationToken ct = default)
    {
        // Lookup is case-insensitive to be friendly, hash compare is constant-time.
        var user = await _users.GetByUsernameAsync(command.Username, ct).ConfigureAwait(false);

        // Run verify even when user is null to avoid timing-based user enumeration.
        var passwordHash = user?.PasswordHash ?? string.Empty;
        var passwordValid = _passwordHasher.Verify(command.Password, passwordHash)
            && user is not null;

        if (!passwordValid || !user!.IsActive)
            throw new InvalidCredentialsException();

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        return new LoginResult(token, expiresAt, "Bearer", user.Username);
    }
}
