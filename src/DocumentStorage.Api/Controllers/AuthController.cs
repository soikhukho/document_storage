using DocumentStorage.Api.Models;
using DocumentStorage.Application.AuthCommands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocumentStorage.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Authenticate an admin user and receive a JWT access token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, LoginResult> handler,
        CancellationToken ct)
    {
        var command = new LoginCommand(request.Username, request.Password);
        var result = await handler.HandleAsync(command, ct);
        return Ok(result);
    }
}
