namespace ChatApp.API.DTOs.Identity;

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? AvatarUrl = null
);

public record LoginDto(
    string Email,
    string Password
);

public record RefreshTokenDto(
    string RefreshToken
);

public record IdentityUserDto(
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    bool IsOnline,
    IEnumerable<string> Roles
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    IdentityUserDto User
);
