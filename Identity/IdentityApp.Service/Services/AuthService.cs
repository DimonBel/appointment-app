using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Entity;
using IdentityApp.Domain.Interfaces;
using IdentityApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityApp.Service.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<AppIdentityUser> userManager,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Response)> RegisterAsync(RegisterDto model)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            return (false, "User with this email already exists", null);
        }

        var existingUserName = await _userManager.FindByNameAsync(model.UserName);
        if (existingUserName != null)
        {
            return (false, "User with this username already exists", null);
        }

        // Create new user
        var user = new AppIdentityUser
        {
            UserName = model.UserName,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, errors, null);
        }

        // Assign default "User" role
        await _userManager.AddToRoleAsync(user, "User");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Get JwtId from access token
        var jwtId = _tokenService.ValidateToken(accessToken);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            JwtId = jwtId ?? Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"))
        };

        await _tokenService.SaveRefreshTokenAsync(refreshTokenEntity);

        var userDto = MapToUserDto(user, roles.ToList());
        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30")),
            User = userDto
        };

        return (true, "Registration successful", response);
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Response)> LoginAsync(LoginDto model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return (false, "Invalid email or password", null);
        }

        if (!user.IsActive)
        {
            return (false, "User account is deactivated", null);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordValid)
        {
            return (false, "Invalid email or password", null);
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        user.IsOnline = true;
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Get JwtId from access token
        var jwtId = _tokenService.ValidateToken(accessToken);

        // Save refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            JwtId = jwtId ?? Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"))
        };

        await _tokenService.SaveRefreshTokenAsync(refreshTokenEntity);

        var userDto = MapToUserDto(user, roles.ToList());
        var response = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30")),
            User = userDto
        };

        return (true, "Login successful", response);
    }

    public async Task<(bool Success, string Message, AuthResponseDto? Response)> RefreshTokenAsync(RefreshTokenDto model)
    {
        // Validate access token
        var jwtId = _tokenService.ValidateToken(model.AccessToken);
        if (jwtId == null)
        {
            return (false, "Invalid access token", null);
        }

        // Get refresh token from database
        var storedToken = await _tokenService.GetRefreshTokenAsync(model.RefreshToken);
        if (storedToken == null)
        {
            return (false, "Invalid refresh token", null);
        }

        // Validate refresh token
        if (storedToken.IsUsed)
        {
            return (false, "Refresh token has been used", null);
        }

        if (storedToken.IsRevoked)
        {
            return (false, "Refresh token has been revoked", null);
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return (false, "Refresh token has expired", null);
        }

        if (storedToken.JwtId != jwtId)
        {
            return (false, "Token mismatch", null);
        }

        // Mark current token as used
        storedToken.IsUsed = true;
        await _unitOfWork.RefreshTokens.UpdateAsync(storedToken);
        await _unitOfWork.SaveChangesAsync();

        // Get user
        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null)
        {
            return (false, "User not found", null);
        }

        // Generate new tokens
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        // Get new JwtId
        var newJwtId = _tokenService.ValidateToken(newAccessToken);

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            JwtId = newJwtId ?? Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7"))
        };

        await _tokenService.SaveRefreshTokenAsync(newRefreshTokenEntity);

        var userDto = MapToUserDto(user, roles.ToList());
        var response = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30")),
            User = userDto
        };

        return (true, "Token refreshed successfully", response);
    }

    public async Task<bool> RevokeTokenAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return false;
        }

        return await _tokenService.RevokeAllUserTokensAsync(userGuid);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var jwtId = _tokenService.ValidateToken(token);
        return jwtId != null;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles.ToList());
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles.ToList());
    }

    private static UserDto MapToUserDto(AppIdentityUser user, List<string> roles)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            IsOnline = user.IsOnline,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles
        };
    }
}
