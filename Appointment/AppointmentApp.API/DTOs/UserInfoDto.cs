namespace AppointmentApp.API.DTOs;

/// <summary>
/// DTO representing basic user information
/// </summary>
public class UserInfoDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}