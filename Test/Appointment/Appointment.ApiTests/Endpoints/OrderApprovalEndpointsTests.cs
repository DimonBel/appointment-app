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
/// API tests for Order Approval endpoints
/// Tests approval, decline, completion, and no-show operations
/// </summary>
[Collection("Api Tests")]
public class OrderApprovalEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public OrderApprovalEndpointsTests()
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

    #region POST /api/orders/{id}/approve - Approve Order

    [Fact]
    public async Task ApproveOrder_WithValidId_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

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

        var client = _factory.CreateAuthenticatedClient(professionalUser.Id, professionalUser.Email, "Professional");

        var approveRequest = new
        {
            reason = "Approved for consultation"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Status.Should().Be(OrderStatus.Approved);
        orderResponse.ApprovalReason.Should().Be("Approved for consultation");

        // Verify order history was created
        var history = await context.OrderHistory.Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().HaveCount(1);
        history.First().NewStatus.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public async Task ApproveOrder_WithInvalidStatus_Returns400BadRequest()
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
            Status = OrderStatus.Completed, // Cannot approve completed order
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(professionalUser.Id, professionalUser.Email, "Professional");

        var approveRequest = new
        {
            reason = "Test approval"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/approve", approveRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/orders/{id}/decline - Decline Order

    [Fact]
    public async Task DeclineOrder_WithValidId_Returns200OK()
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

        var client = _factory.CreateAuthenticatedClient(professionalUser.Id, professionalUser.Email, "Professional");

        var declineRequest = new
        {
            reason = "Professional not available on this date"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/decline", declineRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Status.Should().Be(OrderStatus.Declined);
        orderResponse.DeclineReason.Should().Be("Professional not available on this date");

        // Verify order history
        var history = await context.OrderHistory.Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().HaveCount(1);
        history.First().NewStatus.Should().Be(OrderStatus.Declined);
    }

    #endregion

    #region POST /api/orders/{id}/complete - Complete Order

    [Fact]
    public async Task CompleteOrder_WithValidId_Returns200OK()
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
            Status = OrderStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(professionalUser.Id, professionalUser.Email, "Professional");

        var completeRequest = new
        {
            notes = "Consultation completed successfully"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/complete", completeRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Status.Should().Be(OrderStatus.Completed);
        orderResponse.CompletedAt.Should().NotBeNull();
        orderResponse.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        orderResponse.Notes.Should().Be("Consultation completed successfully");

        // Verify order history
        var history = await context.OrderHistory.Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().HaveCount(1);
        history.First().NewStatus.Should().Be(OrderStatus.Completed);
    }

    #endregion

    #region POST /api/orders/{id}/noshow - Mark as No-Show

    [Fact]
    public async Task MarkAsNoShow_WithValidId_Returns200OK()
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
            Status = OrderStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(professionalUser.Id, professionalUser.Email, "Professional");

        var noShowRequest = new
        {
            notes = "Client did not show up for appointment"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/noshow", noShowRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Status.Should().Be(OrderStatus.NoShow);
        orderResponse.Notes.Should().Be("Client did not show up for appointment");

        // Verify order history
        var history = await context.OrderHistory.Where(h => h.OrderId == order.Id).ToListAsync();
        history.Should().HaveCount(1);
        history.First().NewStatus.Should().Be(OrderStatus.NoShow);
    }

    #endregion

    #region GET /api/orders/{id}/history - Get Order History

    [Fact]
    public async Task GetOrderHistory_WithValidId_Returns200OK()
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
            Status = OrderStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        // Add history entries
        context.OrderHistory.AddRange(
            new OrderHistory { Id = Guid.NewGuid(), OrderId = order.Id, PreviousStatus = OrderStatus.Requested, NewStatus = OrderStatus.Approved, ChangedAt = DateTime.UtcNow, Reason = "Approved" }
        );
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/orders/{order.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<OrderHistory[]>();
        history.Should().NotBeNull();
        history!.Should().HaveCount(1);
        history.First().NewStatus.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public async Task GetOrderHistory_WithNoHistory_ReturnsEmptyArray()
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

        // Act
        var response = await client.GetAsync($"/api/orders/{order.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<OrderHistory[]>();
        history.Should().NotBeNull();
        history!.Should().BeEmpty();
    }

    #endregion
}