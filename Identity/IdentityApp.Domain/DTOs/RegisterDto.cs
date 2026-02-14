using System.ComponentModel.DataAnnotations;

namespace IdentityApp.Domain.DTOs;

public record RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string UserName { get; init; } = string.Empty;

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
}
