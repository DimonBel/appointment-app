using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IdentityApp.API.DTOs;

public record RegisterWithAvatarDto
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

    [Required]
    public string Role { get; init; } = "User";

    public IFormFile? Avatar { get; init; }
}
