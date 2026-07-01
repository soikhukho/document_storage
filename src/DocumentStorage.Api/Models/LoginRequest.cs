using System.ComponentModel.DataAnnotations;

namespace DocumentStorage.Api.Models;

public record LoginRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Username,
    [Required, StringLength(256, MinimumLength = 1)] string Password
);
