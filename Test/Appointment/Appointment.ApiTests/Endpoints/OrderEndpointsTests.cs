using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Appointment.ApiTests.Fixtures;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.ApiTests.Endpoints;

/// <summary>
/// API tests for Order endpoints
/// Tests HTTP requests, responses, status codes, and API contracts
/// </summary>
[Collection("Api Tests")]
public class OrderEndpointsTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public OrderEndpointsTests()
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

    #region POST /api/orders - Create Order

    [Fact]
    public async Task CreateOrder_WithValidData_Returns201Created()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");
        var scheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10);

        var createOrderRequest = new
        {
            professionalId = professional.Id,
            scheduledDateTime = scheduledDateTime.ToString("O"),
            durationMinutes = 60,
            title = "General Consultation",
            description = "Patient has persistent headaches"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Id.Should().NotBeEmpty();
        orderResponse.Status.Should().Be(OrderStatus.Requested);
        orderResponse.ClientId.Should().Be(clientUser.Id);
        orderResponse.ProfessionalId.Should().Be(professionalUser.Id);
        orderResponse.Title.Should().Be("General Consultation");
        orderResponse.Description.Should().Be("Patient has persistent headaches");
        orderResponse.DurationMinutes.Should().Be(60);

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/orders/{orderResponse.Id}");
    }

    [Fact]
    public async Task CreateOrder_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // Not authenticated
        var scheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10);

        var createOrderRequest = new
        {
            professionalId = Guid.NewGuid(),
            scheduledDateTime = scheduledDateTime.ToString("O"),
            durationMinutes = 60
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithPastDateTime_Returns400BadRequest()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");
        var pastDateTime = DateTime.UtcNow.AddHours(-1);

        var createOrderRequest = new
        {
            professionalId = professional.Id,
            scheduledDateTime = pastDateTime.ToString("O"),
            durationMinutes = 60
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidProfessionalId_Returns400BadRequest()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");
        var scheduledDateTime = DateTime.UtcNow.AddDays(7).AddHours(10);

        var createOrderRequest = new
        {
            professionalId = Guid.NewGuid(), // Non-existent
            scheduledDateTime = scheduledDateTime.ToString("O"),
            durationMinutes = 60
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/orders", createOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/orders/{id} - Get Order by ID

    [Fact]
    public async Task GetOrderById_WithValidId_Returns200OK()
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

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/orders/{order.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Id.Should().Be(order.Id);
        orderResponse.Status.Should().Be(OrderStatus.Requested);
    }

    [Fact]
    public async Task GetOrderById_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/orders/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient(); // Not authenticated
        var orderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/orders - Get All Orders

    [Fact]
    public async Task GetAllOrders_WithAuthentication_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        context.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Status = OrderStatus.Requested, CreatedAt = DateTime.UtcNow },
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(2), DurationMinutes = 30, Status = OrderStatus.Approved, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<Order[]>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllOrders_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        context.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Status = OrderStatus.Requested, CreatedAt = DateTime.UtcNow },
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(2), DurationMinutes = 30, Status = OrderStatus.Approved, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync("/api/orders?status=1"); // Status = Approved

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<Order[]>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCount(1);
        orders.First().Status.Should().Be(OrderStatus.Approved);
    }

    [Fact]
    public async Task GetAllOrders_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        for (int i = 0; i < 5; i++)
        {
            context.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                ClientId = clientUser.Id,
                ProfessionalId = professionalUser.Id,
                ScheduledDateTime = DateTime.UtcNow.AddDays(i + 1),
                DurationMinutes = 30,
                Status = OrderStatus.Requested,
                CreatedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync("/api/orders?page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<Order[]>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCount(2);
    }

    #endregion

    #region GET /api/orders/client/{clientId} - Get Orders by Client

    [Fact]
    public async Task GetOrdersByClient_WithValidClientId_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        context.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Status = OrderStatus.Requested, CreatedAt = DateTime.UtcNow },
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(2), DurationMinutes = 30, Status = OrderStatus.Approved, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/orders/client/{clientUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<Order[]>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCount(2);
        orders.All(o => o.ClientId == clientUser.Id).Should().BeTrue();
    }

    #endregion

    #region GET /api/orders/professional/{professionalId} - Get Orders by Professional

    [Fact]
    public async Task GetOrdersByProfessional_WithValidProfessionalId_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);

        var context = await _factory.GetDbContextAsync();
        context.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(1), DurationMinutes = 60, Status = OrderStatus.Requested, CreatedAt = DateTime.UtcNow },
            new Order { Id = Guid.NewGuid(), ClientId = clientUser.Id, ProfessionalId = professionalUser.Id, ScheduledDateTime = DateTime.UtcNow.AddDays(2), DurationMinutes = 30, Status = OrderStatus.Approved, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        // Act
        var response = await client.GetAsync($"/api/orders/professional/{professionalUser.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orders = await response.Content.ReadFromJsonAsync<Order[]>();
        orders.Should().NotBeNull();
        orders!.Should().HaveCount(2);
        orders.All(o => o.ProfessionalId == professionalUser.Id).Should().BeTrue();
    }

    #endregion

    #region PUT /api/orders/{id} - Update Order

    [Fact]
    public async Task UpdateOrder_WithValidData_Returns200OK()
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
            Title = "Original Title",
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        var updateOrderRequest = new
        {
            title = "Updated Title",
            description = "Updated Description",
            notes = "Updated Notes"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/orders/{order.Id}", updateOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Title.Should().Be("Updated Title");
        orderResponse.Description.Should().Be("Updated Description");
        orderResponse.Notes.Should().Be("Updated Notes");
    }

    [Fact]
    public async Task UpdateOrder_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");
        var invalidId = Guid.NewGuid();

        var updateOrderRequest = new
        {
            title = "Updated Title"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/orders/{invalidId}", updateOrderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/orders/{id}/cancel - Cancel Order

    [Fact]
    public async Task CancelOrder_WithValidId_Returns200OK()
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

        var cancelRequest = new
        {
            reason = "Client cancelled appointment"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.Status.Should().Be(OrderStatus.Cancelled);
        orderResponse.Notes.Should().Be("Client cancelled appointment");
    }

    [Fact]
    public async Task CancelOrder_WithInvalidStatus_Returns400BadRequest()
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
            Status = OrderStatus.Completed, // Cannot cancel completed order
            CreatedAt = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");

        var cancelRequest = new
        {
            reason = "Test cancellation"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/cancel", cancelRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/orders/{id}/reschedule - Reschedule Order

    [Fact]
    public async Task RescheduleOrder_WithValidData_Returns200OK()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var professionalUser = await _factory.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        var professional = await _factory.CreateTestProfessionalAsync(professionalUser.Id);
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _factory.CreateTestAvailabilityAsync(professional.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

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

        var newScheduledDateTime = DateTime.UtcNow.AddDays(8).AddHours(14);
        var rescheduleRequest = new
        {
            newScheduledDateTime = newScheduledDateTime.ToString("O"),
            notes = "Rescheduled by client request"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/reschedule", rescheduleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResponse = await response.Content.ReadFromJsonAsync<Order>();
        orderResponse.Should().NotBeNull();
        orderResponse!.ScheduledDateTime.Should().BeCloseTo(newScheduledDateTime, TimeSpan.FromSeconds(1));
        orderResponse.Notes.Should().Be("Rescheduled by client request");
    }

    [Fact]
    public async Task RescheduleOrder_WithPastDateTime_Returns400BadRequest()
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

        var pastDateTime = DateTime.UtcNow.AddHours(-1);
        var rescheduleRequest = new
        {
            newScheduledDateTime = pastDateTime.ToString("O"),
            notes = "Test"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/reschedule", rescheduleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE /api/orders/{id} - Delete Order

    [Fact]
    public async Task DeleteOrder_WithValidId_Returns204NoContent()
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
        var response = await client.DeleteAsync($"/api/orders/{order.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify order was deleted
        var deletedOrder = await context.Orders.FindAsync(order.Id);
        deletedOrder.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOrder_WithInvalidId_Returns404NotFound()
    {
        // Arrange
        var clientUser = await _factory.CreateTestUserAsync("client@test.com", "John", "Doe");
        var client = _factory.CreateAuthenticatedClient(clientUser.Id, clientUser.Email, "Client");
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/orders/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}