using System.ComponentModel.DataAnnotations;

namespace IdentityApp.Domain.DTOs;

public record ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;
}

public record RoleAssignmentDto
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public string RoleName { get; init; } = string.Empty;
}
