namespace DocumentStorage.Application.DTOs;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "DocumentStorage";
    public string Audience { get; set; } = "DocumentStorage";

    /// <summary>Lifetime of access tokens in minutes.</summary>
    public int ExpiresMinutes { get; set; } = 60;
}
