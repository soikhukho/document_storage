using DocumentStorage.Application.AuthCommands;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using NSubstitute;

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
    public async Task HandleAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        var user = AdminUser.Create("admin", "hash");
        var expiresAt = DateTime.UtcNow.AddHours(1);
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);
        _jwt.GenerateToken(user).Returns(("token", expiresAt));

        var result = await _handler.HandleAsync(new LoginCommand("admin", "secret"));

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value!.AccessToken);
        Assert.Equal(expiresAt, result.Value.ExpiresAt);
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal("admin", result.Value.Username);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ReturnsUnauthorizedFailure()
    {
        _users.GetByUsernameAsync("nobody", Arg.Any<CancellationToken>())
            .Returns((AdminUser?)null);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _handler.HandleAsync(new LoginCommand("nobody", "secret"));

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_CREDENTIALS", result.FirstError!.Code);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsUnauthorizedFailure()
    {
        var user = AdminUser.Create("admin", "hash");
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "hash").Returns(false);

        var result = await _handler.HandleAsync(new LoginCommand("admin", "wrong"));

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_CREDENTIALS", result.FirstError!.Code);
    }

    [Fact]
    public async Task HandleAsync_InactiveUser_ReturnsUnauthorizedFailure()
    {
        var user = AdminUser.Create("admin", "hash");
        user.Deactivate();
        _users.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("secret", "hash").Returns(true);

        var result = await _handler.HandleAsync(new LoginCommand("admin", "secret"));

        Assert.True(result.IsFailure);
        Assert.Equal("INVALID_CREDENTIALS", result.FirstError!.Code);
        _jwt.DidNotReceive().GenerateToken(Arg.Any<AdminUser>());
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_StillCallsHasherToAvoidTimingLeak()
    {
        _users.GetByUsernameAsync("ghost", Arg.Any<CancellationToken>())
            .Returns((AdminUser?)null);

        var result = await _handler.HandleAsync(new LoginCommand("ghost", "secret"));

        Assert.True(result.IsFailure);
        _hasher.Received(1).Verify("secret", string.Empty);
    }
}
