namespace SharedDTOs.Identity;

/// <summary>
/// DTO response containing authentication tokens and user information
/// </summary>
public record AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}