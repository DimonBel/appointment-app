using System.ComponentModel.DataAnnotations;

namespace IdentityApp.Domain.DTOs;

public record RefreshTokenDto
{
    [Required]
    public string AccessToken { get; init; } = string.Empty;

    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
