using DocumentStorage.Application.AuthCommands;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using DocumentStorage.Domain.Exceptions;
using NSubstitute;
using System.Security.Cryptography;
using System.Text;

namespace DocumentStorage.Application.Tests.AuthCommands;

public class LoginCommandHandlerTests
{
    private readonly IAdminUserRepository _users = Substitute.For<IAdminUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_users, _hasher, _jwt);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsToken()
    {
        var user = AdminUser.Create("admin", "hash");
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns(("token", expiresAt));

        var result = await _handler.HandleAsync(new LoginCommand("admin", "secret"));

        Assert.Equal("token", result.AccessToken);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ThrowsInvalidCredentials()
    {
        _users.GetByUsernameAsync("nobody", Arg.Any<CancellationToken>())
            .Returns((AdminUser?)null);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _handler.HandleAsync(new LoginCommand("nobody", "secret")));
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ThrowsInvalidCredentials()
    {
        var user = AdminUser.Create("admin", "hash");
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "hash").Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _handler.HandleAsync(new LoginCommand("admin", "wrong")));
    }

    [Fact]
    public async Task HandleAsync_InactiveUser_ThrowsInvalidCredentials()
    {
        var user = AdminUser.Create("admin", "hash");
        user.Deactivate();
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _handler.HandleAsync(new LoginCommand("admin", "secret")));

        // JWT must never be issued for disabled accounts.
        _jwt.DidNotReceive().GenerateToken(Arg.Any<AdminUser>());
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_StillCallsHasherToAvoidTimingLeak()
    {
        _users.GetByUsernameAsync("ghost", Arg.Any<CancellationToken>())
            .Returns((AdminUser?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _handler.HandleAsync(new LoginCommand("ghost", "secret")));

        // Verify that we still ran the password verify step against empty hash,
        // so response time for unknown user is similar to known-but-wrong password.
        _hasher.Received(1).Verify("secret", string.Empty);
    }
}
