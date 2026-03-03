using System.ComponentModel.DataAnnotations;

namespace SharedDTOs.Identity;

/// <summary>
/// DTO for user login
/// </summary>
public record LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}