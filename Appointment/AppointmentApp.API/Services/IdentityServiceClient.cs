using AppointmentApp.API.DTOs.Identity;
using System.Text.Json;

namespace AppointmentApp.API.Services;

public interface IIdentityServiceClient
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto model);
    Task<AuthResponseDto?> LoginAsync(LoginDto model);
    Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto model);
    Task<IdentityUserDto?> GetUserByIdAsync(Guid userId, string accessToken);
    Task<IdentityUserDto?> GetUserByEmailAsync(string email, string accessToken);
    Task<bool> ValidateTokenAsync(string token);
    Task<bool> RevokeTokenAsync(Guid userId, string accessToken);
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

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", model);

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
            return null;
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", model);

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
            return null;
        }
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(RefreshTokenDto model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", model);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Token refresh failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service refresh endpoint");
            return null;
        }
    }

    public async Task<IdentityUserDto?> GetUserByIdAsync(Guid userId, string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Get user by ID failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IdentityUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service get user endpoint");
            return null;
        }
    }

    public async Task<IdentityUserDto?> GetUserByEmailAsync(string email, string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/email/{email}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Get user by email failed: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<IdentityUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service get user by email endpoint");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/validate", token);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("isValid").GetBoolean();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service validate endpoint");
            return false;
        }
    }

    public async Task<bool> RevokeTokenAsync(Guid userId, string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auth/revoke/{userId}");
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service revoke endpoint");
            return false;
        }
    }
}
