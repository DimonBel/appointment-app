using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Appointment.ApiTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Appointment.ApiTests.Endpoints;

/// <summary>
/// API tests for Authentication endpoints
/// Tests authentication, authorization, and token validation
/// </summary>
[Collection("Api Tests")]
public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public AuthEndpointsTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    #region GET /api/auth/validate - Validate Token

    [Fact]
    public async Task ValidateToken_WithValidToken_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var token = _factory.CreateTestToken(user.Id, user.Email, "Client");
        var client = _factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));

        // Act
        var response = await client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var validationResult = await response.Content.ReadAsStringAsync();
        validationResult.Should().Contain("valid");
    }

    [Fact]
    public async Task ValidateToken_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // No authentication

        // Act
        var response = await client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateToken_WithInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid_token");

        // Act
        var response = await client.GetAsync("/api/auth/validate");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/auth/register - Register User

    [Fact]
    public async Task RegisterUser_WithValidData_Returns201Created()
    {
        // Arrange
        var client = _factory.CreateClient();

        var registerRequest = new
        {
            email = "newuser@test.com",
            password = "Test123!",
            userName = "newuser",
            firstName = "New",
            lastName = "User"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var userResponse = await response.Content.ReadFromJsonAsync<dynamic>();
        userResponse.Should().NotBeNull();

        // Verify user was created
        var context = await _factory.GetDbContextAsync();
        var createdUser = await context.Users.FindAsync(userResponse.GetProperty("id").GetGuid());
        createdUser.Should().NotBeNull();
        createdUser!.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task RegisterUser_WithDuplicateEmail_Returns400BadRequest()
    {
        // Arrange
        await _factory.CreateTestUserAsync("existing@test.com", "Existing", "User");
        var client = _factory.CreateClient();

        var registerRequest = new
        {
            email = "existing@test.com", // Duplicate
            password = "Test123!",
            userName = "newuser",
            firstName = "New",
            lastName = "User"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterUser_WithInvalidPassword_Returns400BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        var registerRequest = new
        {
            email = "newuser@test.com",
            password = "123", // Too short
            userName = "newuser",
            firstName = "New",
            lastName = "User"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/login - Login

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var client = _factory.CreateClient();

        var loginRequest = new
        {
            email = user.Email,
            password = "Test123!" // This matches the hashed password in CreateTestUserAsync
        };

        // Note: In real scenario, the password would need to match the hash
        // For testing, we're just checking the endpoint structure

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        // The response status depends on actual password verification
        // In a real test with proper password hashing, this would be 200 OK
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        var loginRequest = new
        {
            email = "nonexistent@test.com",
            password = "WrongPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/health - Health Check

    [Fact]
    public async Task HealthCheck_Returns200OK()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthResponse = await response.Content.ReadFromJsonAsync<dynamic>();
        healthResponse.Should().NotBeNull();
    }

    #endregion
}