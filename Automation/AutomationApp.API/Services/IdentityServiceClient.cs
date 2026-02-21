using System.Text.Json;

namespace AutomationApp.API.Services;

public interface IIdentityServiceClient
{
    Task<Guid?> ValidateTokenAsync(string token);
    Task<string?> GetUserEmailAsync(Guid userId);
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

    public async Task<Guid?> ValidateTokenAsync(string token)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.PostAsJsonAsync("/api/auth/validate", token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token validation failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (result.TryGetProperty("userId", out var userIdElement))
            {
                return userIdElement.GetGuid();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Identity Service validate endpoint");
        }
        return null;
    }

    public async Task<string?> GetUserEmailAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Get user email failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var userData = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (userData.TryGetProperty("email", out var emailElement))
            {
                return emailElement.GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user email");
        }
        return null;
    }
}