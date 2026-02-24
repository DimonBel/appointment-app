using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Appointment.ApiTests.Fixtures;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.ApiTests.Endpoints;

/// <summary>
/// API tests for Availability endpoints
/// Tests availability management and slot generation operations
/// </summary>
[Collection("Api Tests")]
public class AvailabilityEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public AvailabilityEndpointsTests()
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

    #region POST /api/availabilities - Create Availability

    [Fact]
    public async Task CreateAvailability_WithValidData_Returns201Created()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var createAvailabilityRequest = new
        {
            professionalId = professional.Id,
            dayOfWeek = (int)DayOfWeek.Monday,
            startTime = "09:00:00",
            endTime = "17:00:00",
            scheduleType = (int)ScheduleType.Recurring
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/availabilities", createAvailabilityRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var availability = await response.Content.ReadFromJsonAsync<Availability>();
        availability.Should().NotBeNull();
        availability!.Id.Should().NotBeEmpty();
        availability.ProfessionalId.Should().Be(professional.Id);
        availability.DayOfWeek.Should().Be(DayOfWeek.Monday);
        availability.StartTime.Should().Be(TimeSpan.FromHours(9));
        availability.EndTime.Should().Be(TimeSpan.FromHours(17));
        availability.IsActive.Should().BeTrue();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAvailability_WithInvalidTimeRange_Returns400BadRequest()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var createAvailabilityRequest = new
        {
            professionalId = professional.Id,
            dayOfWeek = (int)DayOfWeek.Monday,
            startTime = "17:00:00", // After end time
            endTime = "09:00:00",
            scheduleType = (int)ScheduleType.Recurring
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/availabilities", createAvailabilityRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/availabilities/{id} - Get Availability by ID

    [Fact]
    public async Task GetAvailabilityById_WithValidId_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var availability = await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/availabilities/{availability.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var availabilityResponse = await response.Content.ReadFromJsonAsync<Availability>();
        availabilityResponse.Should().NotBeNull();
        availabilityResponse!.Id.Should().Be(availability.Id);
    }

    #endregion

    #region GET /api/availabilities - Get All Availabilities

    [Fact]
    public async Task GetAllAvailabilities_WithAuthentication_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(13));

        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        // Act
        var response = await client.GetAsync("/api/availabilities");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var availabilities = await response.Content.ReadFromJsonAsync<Availability[]>();
        availabilities.Should().NotBeNull();
        availabilities!.Should().HaveCount(2);
    }

    #endregion

    #region GET /api/availabilities/professional/{professionalId} - Get Availabilities by Professional

    [Fact]
    public async Task GetAvailabilitiesByProfessional_WithValidId_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(13));

        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/availabilities/professional/{professional.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var availabilities = await response.Content.ReadFromJsonAsync<Availability[]>();
        availabilities.Should().NotBeNull();
        availabilities!.Should().HaveCount(2);
        availabilities.All(a => a.ProfessionalId == professional.Id).Should().BeTrue();
    }

    #endregion

    #region PUT /api/availabilities/{id} - Update Availability

    [Fact]
    public async Task UpdateAvailability_WithValidData_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var availability = await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var updateRequest = new
        {
            dayOfWeek = (int)DayOfWeek.Tuesday,
            startTime = "10:00:00",
            endTime = "18:00:00"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/availabilities/{availability.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var availabilityResponse = await response.Content.ReadFromJsonAsync<Availability>();
        availabilityResponse.Should().NotBeNull();
        availabilityResponse!.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        availabilityResponse.StartTime.Should().Be(TimeSpan.FromHours(10));
        availabilityResponse.EndTime.Should().Be(TimeSpan.FromHours(18));
        availabilityResponse.UpdatedAt.Should().NotBeNull();
    }

    #endregion

    #region DELETE /api/availabilities/{id} - Delete Availability

    [Fact]
    public async Task DeleteAvailability_WithValidId_Returns204NoContent()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        var availability = await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        // Act
        var response = await client.DeleteAsync($"/api/availabilities/{availability.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify availability was deleted
        var context = await _factory.GetDbContextAsync();
        var deletedAvailability = await context.Availabilities.FindAsync(availability.Id);
        deletedAvailability.Should().BeNull();
    }

    #endregion

    #region GET /api/availabilities/slots - Get Slots

    [Fact]
    public async Task GetSlotsByDate_WithValidParameters_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");
        var testDate = DateTime.UtcNow.AddDays(((7 - (int)DateTime.UtcNow.DayOfWeek + 7) % 7) + 1); // Next Monday

        // Act
        var response = await client.GetAsync($"/api/availabilities/slots?professionalId={professional.Id}&date={testDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<AvailabilitySlot[]>();
        slots.Should().NotBeNull();
        // Slots will be generated on demand
    }

    [Fact]
    public async Task GetAvailableSlots_WithValidParameters_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(user.Id);
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");
        var testDate = DateTime.UtcNow.AddDays(((7 - (int)DateTime.UtcNow.DayOfWeek + 7) % 7) + 1);

        // Act
        var response = await client.GetAsync($"/api/availabilities/slots/available?professionalId={professional.Id}&date={testDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var slots = await response.Content.ReadFromJsonAsync<AvailabilitySlot[]>();
        slots.Should().NotBeNull();
        slots!.All(s => s.IsAvailable).Should().BeTrue();
    }

    #endregion
}