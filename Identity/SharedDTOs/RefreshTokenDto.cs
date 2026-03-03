using System.ComponentModel.DataAnnotations;

namespace SharedDTOs.Identity;

/// <summary>
/// DTO for refreshing access tokens
/// </summary>
public record RefreshTokenDto
{
    [Required]
    public string AccessToken { get; init; } = string.Empty;

    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}