using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Shared.Results;

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

    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(command.Username, ct).ConfigureAwait(false);

        var passwordHash = user?.PasswordHash ?? string.Empty;
        var passwordValid = _passwordHasher.Verify(command.Password, passwordHash)
            && user is not null;

        if (!passwordValid || !user!.IsActive)
            return Result<LoginResult>.Failure(
                AppError.Unauthorized("INVALID_CREDENTIALS", "Invalid username or password."));

        var (token, expiresAt) = _jwtTokenService.GenerateToken(user);

        return Result<LoginResult>.Success(new LoginResult(token, expiresAt, "Bearer", user.Username));
    }
}
