namespace DocumentStorage.Application.DTOs;

public record LoginResult(
    string AccessToken,
    DateTime ExpiresAt,
    string TokenType,
    string Username);
