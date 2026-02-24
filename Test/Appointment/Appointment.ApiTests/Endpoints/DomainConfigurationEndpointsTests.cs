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
/// API tests for Domain Configuration endpoints
/// Tests domain type configuration operations
/// </summary>
[Collection("Api Tests")]
public class DomainConfigurationEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public DomainConfigurationEndpointsTests()
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

    #region POST /api/domain-configurations - Create Domain Configuration

    [Fact]
    public async Task CreateDomainConfiguration_WithValidData_Returns201Created()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var createRequest = new
        {
            domainType = (int)DomainType.Medical,
            name = "General Medical Consultation",
            description = "Standard medical consultation",
            defaultDurationMinutes = 30
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/domain-configurations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var domainConfig = await response.Content.ReadFromJsonAsync<DomainConfiguration>();
        domainConfig.Should().NotBeNull();
        domainConfig!.Id.Should().NotBeEmpty();
        domainConfig.DomainType.Should().Be(DomainType.Medical);
        domainConfig.Name.Should().Be("General Medical Consultation");
        domainConfig.Description.Should().Be("Standard medical consultation");
        domainConfig.DefaultDurationMinutes.Should().Be(30);
        domainConfig.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateDomainConfiguration_WithInvalidName_Returns400BadRequest()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var createRequest = new
        {
            domainType = (int)DomainType.Medical,
            name = "   ", // Whitespace only
            defaultDurationMinutes = 30
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/domain-configurations", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/domain-configurations - Get All Domain Configurations

    [Fact]
    public async Task GetAllDomainConfigurations_WithAuthentication_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        await _factory.CreateTestDomainConfigurationAsync(DomainType.Medical, "Medical");
        await _factory.CreateTestDomainConfigurationAsync(DomainType.Legal, "Legal");

        // Act
        var response = await client.GetAsync("/api/domain-configurations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var configs = await response.Content.ReadFromJsonAsync<DomainConfiguration[]>();
        configs.Should().NotBeNull();
        configs!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllDomainConfigurations_WithOnlyActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Client");

        var config1 = await _factory.CreateTestDomainConfigurationAsync(DomainType.Medical, "Active");
        var config2 = await _factory.CreateTestDomainConfigurationAsync(DomainType.Legal, "Inactive");

        var context = await _factory.GetDbContextAsync();
        config2.IsActive = false;
        await context.SaveChangesAsync();

        // Act
        var response = await client.GetAsync("/api/domain-configurations?onlyActive=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var configs = await response.Content.ReadFromJsonAsync<DomainConfiguration[]>();
        configs.Should().NotBeNull();
        configs!.Should().HaveCount(1);
        configs.First().Id.Should().Be(config1.Id);
    }

    #endregion

    #region PUT /api/domain-configurations/{id} - Update Domain Configuration

    [Fact]
    public async Task UpdateDomainConfiguration_WithValidData_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var config = await _factory.CreateTestDomainConfigurationAsync(DomainType.Medical, "Original Name");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        var updateRequest = new
        {
            name = "Updated Name",
            description = "Updated Description",
            defaultDurationMinutes = 45
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/domain-configurations/{config.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var configResponse = await response.Content.ReadFromJsonAsync<DomainConfiguration>();
        configResponse.Should().NotBeNull();
        configResponse!.Name.Should().Be("Updated Name");
        configResponse.Description.Should().Be("Updated Description");
        configResponse.DefaultDurationMinutes.Should().Be(45);
        configResponse.UpdatedAt.Should().NotBeNull();
    }

    #endregion

    #region POST /api/domain-configurations/{id}/activate - Activate Domain Configuration

    [Fact]
    public async Task ActivateDomainConfiguration_WithValidId_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var config = await _factory.CreateTestDomainConfigurationAsync(DomainType.Medical, "Test");
        
        var context = await _factory.GetDbContextAsync();
        config.IsActive = false;
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        // Act
        var response = await client.PostAsync($"/api/domain-configurations/{config.Id}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var configResponse = await response.Content.ReadFromJsonAsync<DomainConfiguration>();
        configResponse.Should().NotBeNull();
        configResponse!.IsActive.Should().BeTrue();
    }

    #endregion

    #region POST /api/domain-configurations/{id}/deactivate - Deactivate Domain Configuration

    [Fact]
    public async Task DeactivateDomainConfiguration_WithValidId_Returns200OK()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var config = await _factory.CreateTestDomainConfigurationAsync(DomainType.Medical, "Test");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        // Act
        var response = await client.PostAsync($"/api/domain-configurations/{config.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var configResponse = await response.Content.ReadFromJsonAsync<DomainConfiguration>();
        configResponse.Should().NotBeNull();
        configResponse!.IsActive.Should().BeFalse();
    }

    #endregion

    #region DELETE /api/domain-configurations/{id} - Delete Domain Configuration

    [Fact]
    public async Task DeleteDomainConfiguration_WithValidId_Returns204NoContent()
    {
        // Arrange
        var user = await _factory.CreateTestUserAsync("admin@test.com", "Admin", "User");
        var config = await _factory.CreateTestDomainConfigurationAsync(DomainType.Medical, "Test");
        var client = _factory.CreateAuthenticatedClient(user.Id, user.Email, "Admin");

        // Act
        var response = await client.DeleteAsync($"/api/domain-configurations/{config.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var context = await _factory.GetDbContextAsync();
        var deletedConfig = await context.DomainConfigurations.FindAsync(config.Id);
        deletedConfig.Should().BeNull();
    }

    #endregion
}