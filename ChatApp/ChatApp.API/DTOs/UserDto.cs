namespace ChatApp.API.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; }
}