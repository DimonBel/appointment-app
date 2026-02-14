using ChatApp.API.DTOs.Identity;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ChatApp.API.Services;

public interface IIdentityServiceClient
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshDto);
    Task<IdentityUserDto?> GetUserByIdAsync(string userId, string accessToken);
    Task<bool> ValidateTokenAsync(string accessToken);
    Task<bool> RevokeTokenAsync(string refreshToken, string accessToken);
}

public class IdentityServiceClient : IIdentityServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityServiceClient> _logger;

    public IdentityServiceClient(HttpClient httpClient, ILogger<IdentityServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", registerDto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Registration failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service register endpoint");
            throw;
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginDto);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service login endpoint");
            throw;
        }
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", refreshDto);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service refresh endpoint");
            throw;
        }
    }

    public async Task<IdentityUserDto?> GetUserByIdAsync(string userId, string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync($"/api/users/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Get user failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IdentityUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service get user endpoint");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string accessToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/validate", new { Token = accessToken });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token with Identity Service");
            return false;
        }
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.PostAsJsonAsync("/api/auth/revoke", new RefreshTokenDto(refreshToken));
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token with Identity Service");
            return false;
        }
    }
}
