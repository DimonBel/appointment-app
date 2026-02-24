using System;
using System.Collections.Generic;
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
/// API tests for Pre-Order Data endpoints
/// Tests pre-order data collection and validation operations
/// </summary>
[Collection("Api Tests")]
public class PreOrderDataEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public PreOrderDataEndpointsTests()
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

    #region POST /api/preorder-data - Create Pre-Order Data

    [Fact]
    public async Task CreatePreOrderData_WithValidData_Returns201Created()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientUser.Id,
            ProfessionalId = professionalUser.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10),
            DurationMinutes = 60,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" },
            { "duration", "2 weeks" }
        };

        var createRequest = new
        {
            orderId = order.Id,
            clientId = clientUser.Id,
            dataFields
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/preorder-data", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var preOrderData = await response.Content.ReadFromJsonAsync<PreOrderData>();
        preOrderData.Should().NotBeNull();
        preOrderData!.Id.Should().NotBeEmpty();
        preOrderData.OrderId.Should().Be(order.Id);
        preOrderData.ClientId.Should().Be(clientUser.Id);
        preOrderData.DataFields.Should().HaveCount(2);
        preOrderData.IsCompleted.Should().BeFalse();
    }

    #endregion

    #region GET /api/preorder-data/order/{orderId} - Get Pre-Order Data by Order

    [Fact]
    public async Task GetPreOrderDataByOrder_WithValidOrderId_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientUser.Id,
            ProfessionalId = professionalUser.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10),
            DurationMinutes = 60,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ClientId = clientUser.Id,
            DataFields = new Dictionary<string, string> { { "symptoms", "Test" } },
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.PreOrderData.Add(preOrderData);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/preorder-data/order/{order.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dataResponse = await response.Content.ReadFromJsonAsync<PreOrderData>();
        dataResponse.Should().NotBeNull();
        dataResponse!.OrderId.Should().Be(order.Id);
    }

    #endregion

    #region PUT /api/preorder-data/{id} - Update Pre-Order Data

    [Fact]
    public async Task UpdatePreOrderData_WithValidData_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientUser.Id,
            ProfessionalId = professionalUser.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10),
            DurationMinutes = 60,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ClientId = clientUser.Id,
            DataFields = new Dictionary<string, string> { { "symptoms", "Headache" } },
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.PreOrderData.Add(preOrderData);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        var updateFields = new Dictionary<string, string>
        {
            { "symptoms", "Migraine" },
            { "duration", "2 weeks" }
        };

        var updateRequest = new
        {
            dataFields = updateFields
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/preorder-data/{preOrderData.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dataResponse = await response.Content.ReadFromJsonAsync<PreOrderData>();
        dataResponse.Should().NotBeNull();
        dataResponse!.DataFields.Should().HaveCount(2);
        dataResponse.DataFields["symptoms"].Should().Be("Migraine");
        dataResponse.UpdatedAt.Should().NotBeNull();
    }

    #endregion

    #region POST /api/preorder-data/{id}/complete - Mark as Completed

    [Fact]
    public async Task MarkPreOrderDataAsCompleted_WithValidId_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientUser.Id,
            ProfessionalId = professionalUser.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10),
            DurationMinutes = 60,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ClientId = clientUser.Id,
            DataFields = new Dictionary<string, string> { { "symptoms", "Test" } },
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.PreOrderData.Add(preOrderData);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.PostAsync($"/api/preorder-data/{preOrderData.Id}/complete", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dataResponse = await response.Content.ReadFromJsonAsync<PreOrderData>();
        dataResponse.Should().NotBeNull();
        dataResponse!.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region POST /api/preorder-data/{id}/validate - Validate Pre-Order Data

    [Fact]
    public async Task ValidatePreOrderData_WithAllRequiredFields_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientUser.Id,
            ProfessionalId = professionalUser.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10),
            DurationMinutes = 60,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ClientId = clientUser.Id,
            DataFields = new Dictionary<string, string>
            {
                { "symptoms", "Headache" },
                { "duration", "2 weeks" },
                { "severity", "Moderate" }
            },
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.PreOrderData.Add(preOrderData);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" },
            { "severity", "" }
        };

        var validateRequest = new
        {
            requiredFields
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/preorder-data/{preOrderData.Id}/validate", validateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var validationResult = await response.Content.ReadFromJsonAsync<dynamic>();
        validationResult.Should().NotBeNull();
        // Response should indicate validation passed
    }

    #endregion

    #region DELETE /api/preorder-data/{id} - Delete Pre-Order Data

    [Fact]
    public async Task DeletePreOrderData_WithValidId_Returns204NoContent()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientUser.Id,
            ProfessionalId = professionalUser.Id,
            ScheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10),
            DurationMinutes = 60,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var preOrderData = new PreOrderData
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ClientId = clientUser.Id,
            DataFields = new Dictionary<string, string>(),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        context.PreOrderData.Add(preOrderData);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Admin");

        // Act
        var response = await client.DeleteAsync($"/api/preorder-data/{preOrderData.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var deletedData = await context.PreOrderData.FindAsync(preOrderData.Id);
        deletedData.Should().BeNull();
    }

    #endregion
}