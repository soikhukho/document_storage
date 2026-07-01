using System.Security.Claims;
using System.Text;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DocumentStorage.Infrastructure.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    public const string AdminUserIdClaim = "admin_uid";
    public const string AdminRoleClaim = "role";
    public const string AdminRoleValue = "admin";

    private const string SigningSecret = "D0cuSt0r@geJwt$ecretK3y_2024!L0ngS3cur3";

    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningSecret));
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(AdminUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(AdminUserIdClaim, user.Id.ToString()),
            new(AdminRoleClaim, AdminRoleValue),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                _signingKey, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return (token, expiresAt);
    }

    public async Task<Guid?> ValidateTokenAsync(string token)
    {
        var handler = new JsonWebTokenHandler();
        if (!handler.CanReadToken(token))
            return null;

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(_options.Issuer),
            ValidIssuer = _options.Issuer,
            ValidateAudience = !string.IsNullOrEmpty(_options.Audience),
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var result = await handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
            return null;

        var userIdClaim = result.ClaimsIdentity?.FindFirst(AdminUserIdClaim)?.Value;
        return Guid.TryParse(userIdClaim, out var id) ? id : null;
    }
}
