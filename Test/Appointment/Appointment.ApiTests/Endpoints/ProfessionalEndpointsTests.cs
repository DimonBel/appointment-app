using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Appointment.ApiTests.Fixtures;
using AppointmentApp.Domain.Entity;
using FluentAssertions;
using Xunit;

namespace Appointment.ApiTests.Endpoints;

/// <summary>
/// API tests for Professional endpoints
/// Tests professional profile management operations
/// </summary>
[Collection("Api Tests")]
public class ProfessionalEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public ProfessionalEndpointsTests()
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

    #region POST /api/professionals - Create Professional

    [Fact]
    public async Task CreateProfessional_WithValidData_Returns201Created()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var createProfessionalRequest = new
        {
            userId = user.Id,
            title = "Dr.",
            qualifications = "MD, Board Certified",
            specialization = "Family Medicine"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/professionals", createProfessionalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var professional = await response.Content.ReadFromJsonAsync<Professional>();
        professional.Should().NotBeNull();
        professional!.Id.Should().NotBeEmpty();
        professional.UserId.Should().Be(user.Id);
        professional.Title.Should().Be("Dr.");
        professional.Qualifications.Should().Be("MD, Board Certified");
        professional.Specialization.Should().Be("Family Medicine");
        professional.IsAvailable.Should().BeTrue();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/professionals/{professional.Id}");
    }

    [Fact]
    public async Task CreateProfessional_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // Not authenticated
        var userId = Guid.NewGuid();

        var createProfessionalRequest = new
        {
            userId,
            title = "Dr."
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/professionals", createProfessionalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProfessional_WithDuplicateUserId_Returns400BadRequest()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        await _factory.CreateTestProfessionalAsync(user.Id); // First professional

        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var createProfessionalRequest = new
        {
            userId = user.Id,
            title = "Dr."
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/professionals", createProfessionalRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/professionals/{id} - Get Professional by ID

    [Fact]
    public async Task GetProfessionalById_WithValidId_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/professionals/{professional.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var professionalResponse = await response.Content.ReadFromJsonAsync<Professional>();
        professionalResponse.Should().NotBeNull();
        professionalResponse!.Id.Should().Be(professional.Id);
        professionalResponse.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetProfessionalById_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/professionals/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/professionals - Get All Professionals

    [Fact]
    public async Task GetAllProfessionals_WithAuthentication_Returns200OK()
    {
        // Arrange
        var user1 = await _factory.CreateTestUserAsync("doctor1@test.com", "Dr. Jane", "Smith");
        var user2 = await _factory.CreateTestUserAsync("doctor2@test.com", "Dr. Bob", "Johnson");
        
        await _factory.CreateTestProfessionalAsync(user1.Id);
        await _factory.CreateTestProfessionalAsync(user2.Id);

        var client = _factory.CreateAuthenticatedClient(user1.Id, user1.Email, "Client");

        // Act
        var response = await client.GetAsync("/api/professionals");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var professionals = await response.Content.ReadFromJsonAsync<Professional[]>();
        professionals.Should().NotBeNull();
        professionals!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllProfessionals_WithAvailableFilter_ReturnsFilteredResults()
    {
        // Arrange
        var user1 = await _factory.CreateTestUserAsync("doctor1@test.com", "Dr. Jane", "Smith");
        var user2 = await _factory.CreateTestUserAsync("doctor2@test.com", "Dr. Bob", "Johnson");
        
        var professional1 = await _factory.CreateTestProfessionalAsync(user1.Id);
        var professional2 = await _factory.CreateTestProfessionalAsync(user2.Id);

        var context = await _factory.GetDbContextAsync();
        professional2.IsAvailable = false;
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(user1.Id, user1.Email, "Client");

        // Act
        var response = await client.GetAsync("/api/professionals?onlyAvailable=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var professionals = await response.Content.ReadFromJsonAsync<Professional[]>();
        professionals.Should().NotBeNull();
        professionals!.Should().HaveCount(1);
        professionals.First().Id.Should().Be(professional1.Id);
    }

    #endregion

    #region GET /api/professionals/user/{userId} - Get Professional by User ID

    [Fact]
    public async Task GetProfessionalByUserId_WithValidUserId_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/professionals/user/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var professionalResponse = await response.Content.ReadFromJsonAsync<Professional>();
        professionalResponse.Should().NotBeNull();
        professionalResponse!.UserId.Should().Be(user.Id);
    }

    #endregion

    #region PUT /api/professionals/{id} - Update Professional

    [Fact]
    public async Task UpdateProfessional_WithValidData_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var updateRequest = new
        {
            title = "Dr.",
            qualifications = "MD, PhD",
            specialization = "Internal Medicine",
            hourlyRate = 150,
            experienceYears = 10,
            bio = "Experienced physician"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/professionals/{professional.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var professionalResponse = await response.Content.ReadFromJsonAsync<Professional>();
        professionalResponse.Should().NotBeNull();
        professionalResponse!.Qualifications.Should().Be("MD, PhD");
        professionalResponse.Specialization.Should().Be("Internal Medicine");
        professionalResponse.HourlyRate.Should().Be(150);
        professionalResponse.ExperienceYears.Should().Be(10);
        professionalResponse.Bio.Should().Be("Experienced physician");
        professionalResponse.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateProfessional_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");
        var invalidId = Guid.NewGuid();

        var updateRequest = new
        {
            title = "Dr."
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/professionals/{invalidId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PATCH /api/professionals/{id}/availability - Set Professional Availability

    [Fact]
    public async Task SetProfessionalAvailability_WithValidData_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var availabilityRequest = new
        {
            isAvailable = false
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/professionals/{professional.Id}/availability", availabilityRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var professionalResponse = await response.Content.ReadFromJsonAsync<Professional>();
        professionalResponse.Should().NotBeNull();
        professionalResponse!.IsAvailable.Should().BeFalse();
    }

    #endregion

    #region DELETE /api/professionals/{id} - Delete Professional

    [Fact]
    public async Task DeleteProfessional_WithValidId_Returns204NoContent()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        // Act
        var response = await client.DeleteAsync($"/api/professionals/{professional.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify professional was deleted
        var context = await _factory.GetDbContextAsync();
        var deletedProfessional = await context.Professionals.FindAsync(professional.Id);
        deletedProfessional.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfessional_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/professionals/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}