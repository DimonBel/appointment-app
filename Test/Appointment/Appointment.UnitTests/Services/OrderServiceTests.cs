using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Appointment.UnitTests.Services;

/// <summary>
/// Comprehensive unit tests for OrderService covering all scenarios and functionality
/// Module: Order Management Module (1.1)
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IProfessionalRepository> _mockProfessionalRepository;
    private readonly Mock<IAvailabilitySlotRepository> _mockAvailabilitySlotRepository;
    private readonly Mock<UserManager<AppIdentityUser>> _mockUserManager;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockProfessionalRepository = new Mock<IProfessionalRepository>();
        _mockAvailabilitySlotRepository = new Mock<IAvailabilitySlotRepository>();
        
        var store = new Mock<IUserStore<AppIdentityUser>>();
        _mockUserManager = new Mock<UserManager<AppIdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        _orderService = new OrderService(
            _mockOrderRepository.Object,
            _mockProfessionalRepository.Object,
            _mockAvailabilitySlotRepository.Object,
            _mockUserManager.Object);
    }

    #region CreateOrderAsync Tests

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        const int durationMinutes = 60;
        const string title = "Consultation";
        const string description = "Initial consultation";

        var professional = new Professional
        {
            Id = professionalId,
            UserId = Guid.NewGuid(),
            IsAvailable = true
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professional.UserId,
            ScheduledDateTime = scheduledDateTime,
            DurationMinutes = durationMinutes,
            Title = title,
            Description = description,
            Status = OrderStatus.Requested
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync(new AppIdentityUser { Id = clientId });
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalId, scheduledDateTime, durationMinutes))
            .ReturnsAsync(true);
        _mockOrderRepository.Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _mockOrderRepository.Setup(x => x.GetByIdAsync(createdOrder.Id))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _orderService.CreateOrderAsync(
            clientId, professionalId, scheduledDateTime, durationMinutes, title, description);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Requested);
        result.ClientId.Should().Be(clientId);
        result.DurationMinutes.Should().Be(durationMinutes);
        result.Title.Should().Be(title);
        result.Description.Should().Be(description);

        _mockOrderRepository.Verify(x => x.CreateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentClient_ShouldCreateShadowUser()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        const int durationMinutes = 60;

        var professional = new Professional
        {
            Id = professionalId,
            UserId = Guid.NewGuid(),
            IsAvailable = true
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professional.UserId,
            ScheduledDateTime = scheduledDateTime,
            DurationMinutes = durationMinutes,
            Status = OrderStatus.Requested
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync((AppIdentityUser?)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppIdentityUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalId, scheduledDateTime, durationMinutes))
            .ReturnsAsync(true);
        _mockOrderRepository.Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _mockOrderRepository.Setup(x => x.GetByIdAsync(createdOrder.Id))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _orderService.CreateOrderAsync(
            clientId, professionalId, scheduledDateTime, durationMinutes);

        // Assert
        result.Should().NotBeNull();
        _mockUserManager.Verify(x => x.CreateAsync(It.Is<AppIdentityUser>(u => u.Id == clientId)), Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentProfessional_ShouldThrowArgumentException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        const int durationMinutes = 60;

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync(new AppIdentityUser { Id = clientId });
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync((Professional?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderService.CreateOrderAsync(clientId, professionalId, scheduledDateTime, durationMinutes));

        // Assert
        exception.ParamName.Should().Be(nameof(professionalId));
        exception.Message.Should().Contain("Professional not found");
    }

    [Fact]
    public async Task CreateOrderAsync_WithUnavailableProfessional_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        const int durationMinutes = 60;

        var professional = new Professional
        {
            Id = professionalId,
            UserId = Guid.NewGuid(),
            IsAvailable = false
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync(new AppIdentityUser { Id = clientId });
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(clientId, professionalId, scheduledDateTime, durationMinutes));

        // Assert
        exception.Message.Should().Contain("Professional is not available for booking");
    }

    [Fact]
    public async Task CreateOrderAsync_WithUnavailableTimeSlot_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        const int durationMinutes = 60;

        var professional = new Professional
        {
            Id = professionalId,
            UserId = Guid.NewGuid(),
            IsAvailable = true
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync(new AppIdentityUser { Id = clientId });
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalId, scheduledDateTime, durationMinutes))
            .ReturnsAsync(false);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(clientId, professionalId, scheduledDateTime, durationMinutes));

        // Assert
        exception.Message.Should().Contain("Requested time slot is not available");
    }

    [Fact]
    public async Task CreateOrderAsync_WithDomainConfigurationId_ShouldIncludeConfiguration()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.UtcNow.AddDays(1);
        const int durationMinutes = 60;
        var domainConfigurationId = Guid.NewGuid();

        var professional = new Professional
        {
            Id = professionalId,
            UserId = Guid.NewGuid(),
            IsAvailable = true
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professional.UserId,
            ScheduledDateTime = scheduledDateTime,
            DurationMinutes = durationMinutes,
            DomainConfigurationId = domainConfigurationId,
            Status = OrderStatus.Requested
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync(new AppIdentityUser { Id = clientId });
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalId, scheduledDateTime, durationMinutes))
            .ReturnsAsync(true);
        _mockOrderRepository.Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _mockOrderRepository.Setup(x => x.GetByIdAsync(createdOrder.Id))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _orderService.CreateOrderAsync(
            clientId, professionalId, scheduledDateTime, durationMinutes,
            domainConfigurationId: domainConfigurationId);

        // Assert
        result.DomainConfigurationId.Should().Be(domainConfigurationId);
    }

    [Fact]
    public async Task CreateOrderAsync_WithLocalDateTime_ShouldNormalizeToUtc()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var scheduledDateTime = DateTime.Now.AddDays(1); // Local time
        const int durationMinutes = 60;

        var professional = new Professional
        {
            Id = professionalId,
            UserId = Guid.NewGuid(),
            IsAvailable = true
        };

        var createdOrder = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professional.UserId,
            ScheduledDateTime = scheduledDateTime.ToUniversalTime(),
            DurationMinutes = durationMinutes,
            Status = OrderStatus.Requested
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(clientId.ToString()))
            .ReturnsAsync(new AppIdentityUser { Id = clientId });
        _mockProfessionalRepository.Setup(x => x.GetByIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalId, It.IsAny<DateTime>(), durationMinutes))
            .ReturnsAsync(true);
        _mockOrderRepository.Setup(x => x.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync(createdOrder);
        _mockOrderRepository.Setup(x => x.GetByIdAsync(createdOrder.Id))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _orderService.CreateOrderAsync(
            clientId, professionalId, scheduledDateTime, durationMinutes);

        // Assert
        result.ScheduledDateTime.Kind.Should().Be(DateTimeKind.Utc);
        _mockAvailabilitySlotRepository.Verify(x => x.IsSlotAvailableAsync(
            professionalId,
            It.Is<DateTime>(dt => dt.Kind == DateTimeKind.Utc),
            durationMinutes), Times.Once);
    }

    #endregion

    #region GetOrderByIdAsync Tests

    [Fact]
    public async Task GetOrderByIdAsync_WithExistingOrder_ShouldReturnOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, Status = OrderStatus.Requested };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
        _mockOrderRepository.Verify(x => x.GetByIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithNonExistentOrder_ShouldReturnNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllOrdersAsync Tests

    [Fact]
    public async Task GetAllOrdersAsync_WithoutFilters_ShouldReturnAllOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), Status = OrderStatus.Requested },
            new Order { Id = Guid.NewGuid(), Status = OrderStatus.Approved },
            new Order { Id = Guid.NewGuid(), Status = OrderStatus.Completed }
        };

        _mockOrderRepository.Setup(x => x.GetAllAsync(null, 1, 100, null, false))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetAllOrdersAsync();

        // Assert
        result.Should().HaveCount(3);
        _mockOrderRepository.Verify(x => x.GetAllAsync(null, 1, 100, null, false), Times.Once);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithStatusFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), Status = OrderStatus.Requested },
            new Order { Id = Guid.NewGuid(), Status = OrderStatus.Requested }
        };

        _mockOrderRepository.Setup(x => x.GetAllAsync(OrderStatus.Requested, 1, 100, null, false))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetAllOrdersAsync(OrderStatus.Requested);

        // Assert
        result.Should().HaveCount(2);
        result.All(o => o.Status == OrderStatus.Requested).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithPagination_ShouldApplyPagination()
    {
        // Arrange
        var orders = new List<Order> { new Order { Id = Guid.NewGuid() } };

        _mockOrderRepository.Setup(x => x.GetAllAsync(null, 2, 50, null, false))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetAllOrdersAsync(page: 2, pageSize: 50);

        // Assert
        _mockOrderRepository.Verify(x => x.GetAllAsync(null, 2, 50, null, false), Times.Once);
    }

    [Fact]
    public async Task GetAllOrdersAsync_WithSorting_ShouldApplySorting()
    {
        // Arrange
        var orders = new List<Order> { new Order { Id = Guid.NewGuid() } };

        _mockOrderRepository.Setup(x => x.GetAllAsync(null, 1, 100, "ScheduledDateTime", true))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetAllOrdersAsync(sortBy: "ScheduledDateTime", descending: true);

        // Assert
        _mockOrderRepository.Verify(x => x.GetAllAsync(null, 1, 100, "ScheduledDateTime", true), Times.Once);
    }

    #endregion

    #region GetOrdersByClientAsync Tests

    [Fact]
    public async Task GetOrdersByClientAsync_WithValidClientId_ShouldReturnClientOrders()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), ClientId = clientId, Status = OrderStatus.Requested },
            new Order { Id = Guid.NewGuid(), ClientId = clientId, Status = OrderStatus.Approved }
        };

        _mockOrderRepository.Setup(x => x.GetByClientAsync(clientId, null, 1, 20))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetOrdersByClientAsync(clientId);

        // Assert
        result.Should().HaveCount(2);
        result.All(o => o.ClientId == clientId).Should().BeTrue();
    }

    [Fact]
    public async Task GetOrdersByClientAsync_WithStatusFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), ClientId = clientId, Status = OrderStatus.Requested }
        };

        _mockOrderRepository.Setup(x => x.GetByClientAsync(clientId, OrderStatus.Requested, 1, 20))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetOrdersByClientAsync(clientId, OrderStatus.Requested);

        // Assert
        result.Should().HaveCount(1);
        result.All(o => o.Status == OrderStatus.Requested).Should().BeTrue();
    }

    #endregion

    #region GetOrdersByProfessionalAsync Tests

    [Fact]
    public async Task GetOrdersByProfessionalAsync_WithValidProfessionalId_ShouldReturnProfessionalOrders()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), ProfessionalId = professionalId, Status = OrderStatus.Requested },
            new Order { Id = Guid.NewGuid(), ProfessionalId = professionalId, Status = OrderStatus.Approved }
        };

        _mockOrderRepository.Setup(x => x.GetByProfessionalAsync(professionalId, null, 1, 20))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetOrdersByProfessionalAsync(professionalId);

        // Assert
        result.Should().HaveCount(2);
        result.All(o => o.ProfessionalId == professionalId).Should().BeTrue();
    }

    [Fact]
    public async Task GetOrdersByProfessionalAsync_WithStatusFilter_ShouldReturnFilteredOrders()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), ProfessionalId = professionalId, Status = OrderStatus.Approved }
        };

        _mockOrderRepository.Setup(x => x.GetByProfessionalAsync(professionalId, OrderStatus.Approved, 1, 20))
            .ReturnsAsync(orders);

        // Act
        var result = await _orderService.GetOrdersByProfessionalAsync(professionalId, OrderStatus.Approved);

        // Assert
        result.Should().HaveCount(1);
        result.All(o => o.Status == OrderStatus.Approved).Should().BeTrue();
    }

    #endregion

    #region GetClientsByProfessionalAsync Tests

    [Fact]
    public async Task GetClientsByProfessionalAsync_WithValidProfessionalId_ShouldReturnClients()
    {
        // Arrange
        var professionalId = Guid.NewGuid();
        var clients = new List<AppIdentityUser>
        {
            new AppIdentityUser { Id = Guid.NewGuid(), FirstName = "Client1" },
            new AppIdentityUser { Id = Guid.NewGuid(), FirstName = "Client2" }
        };

        _mockOrderRepository.Setup(x => x.GetClientsByProfessionalAsync(professionalId))
            .ReturnsAsync(clients);

        // Act
        var result = await _orderService.GetClientsByProfessionalAsync(professionalId);

        // Assert
        result.Should().HaveCount(2);
        _mockOrderRepository.Verify(x => x.GetClientsByProfessionalAsync(professionalId), Times.Once);
    }

    #endregion

    #region UpdateOrderAsync Tests

    [Fact]
    public async Task UpdateOrderAsync_WithValidData_ShouldUpdateOrderSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string newTitle = "Updated Title";
        const string newDescription = "Updated Description";
        const string newNotes = "Updated Notes";

        var existingOrder = new Order
        {
            Id = orderId,
            Title = "Original Title",
            Description = "Original Description",
            Notes = "Original Notes",
            Status = OrderStatus.Requested
        };

        var updatedOrder = new Order
        {
            Id = orderId,
            Title = newTitle,
            Description = newDescription,
            Notes = newNotes,
            Status = OrderStatus.Requested
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(updatedOrder);

        // Act
        var result = await _orderService.UpdateOrderAsync(orderId, newTitle, newDescription, newNotes);

        // Assert
        result.Title.Should().Be(newTitle);
        result.Description.Should().Be(newDescription);
        result.Notes.Should().Be(newNotes);
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_WithPartialData_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string newTitle = "Updated Title";

        var existingOrder = new Order
        {
            Id = orderId,
            Title = "Original Title",
            Description = "Original Description",
            Status = OrderStatus.Requested
        };

        var updatedOrder = new Order
        {
            Id = orderId,
            Title = newTitle,
            Description = "Original Description",
            Status = OrderStatus.Requested
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(updatedOrder);

        // Act
        var result = await _orderService.UpdateOrderAsync(orderId, newTitle);

        // Assert
        result.Title.Should().Be(newTitle);
        result.Description.Should().Be("Original Description");
    }

    [Fact]
    public async Task UpdateOrderAsync_WithNonExistentOrder_ShouldThrowArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderService.UpdateOrderAsync(orderId, "New Title"));

        // Assert
        exception.ParamName.Should().Be(nameof(orderId));
        exception.Message.Should().Contain("Order not found");
    }

    #endregion

    #region CancelOrderAsync Tests

    [Fact]
    public async Task CancelOrderAsync_WithRequestedStatus_ShouldCancelSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string reason = "Client cancelled";

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ProfessionalId = Guid.NewGuid(),
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var cancelledOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Cancelled,
            Notes = reason,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(cancelledOrder);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, reason);

        // Assert
        result.Status.Should().Be(OrderStatus.Cancelled);
        result.Notes.Should().Be(reason);
        result.UpdatedAt.Should().NotBeNull();
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WithApprovedStatus_ShouldCancelAndReleaseSlots()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var professionalEntityId = Guid.NewGuid();
        const string reason = "Professional cancelled";

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved,
            ProfessionalId = professionalId,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var professional = new Professional
        {
            Id = professionalEntityId,
            UserId = professionalId,
            IsAvailable = true
        };

        var slot1 = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            AvailabilityId = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            IsAvailable = false
        };

        var slot2 = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            AvailabilityId = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            EndTime = TimeSpan.FromHours(11),
            IsAvailable = false
        };

        var cancelledOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Cancelled,
            Notes = reason,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(
            professionalEntityId, It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { slot1, slot2 });
        _mockAvailabilitySlotRepository.Setup(x => x.UpdateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(cancelledOrder);

        // Act
        var result = await _orderService.CancelOrderAsync(orderId, reason);

        // Assert
        result.Status.Should().Be(OrderStatus.Cancelled);
        _mockProfessionalRepository.Verify(x => x.GetByUserIdAsync(professionalId), Times.Once);
        _mockAvailabilitySlotRepository.Verify(x => x.UpdateAsync(It.IsAny<AvailabilitySlot>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CancelOrderAsync_WithCompletedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CancelOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot cancel order with status Completed");
    }

    [Fact]
    public async Task CancelOrderAsync_WithCancelledStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Cancelled
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CancelOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot cancel order with status Cancelled");
    }

    [Fact]
    public async Task CancelOrderAsync_WithDeclinedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Declined
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CancelOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot cancel order with status Declined");
    }

    #endregion

    #region RescheduleOrderAsync Tests

    [Fact]
    public async Task RescheduleOrderAsync_WithRequestedStatus_ShouldRescheduleSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var newScheduledDateTime = DateTime.UtcNow.AddDays(2);
        const string notes = "Rescheduled by client";

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var rescheduledOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ScheduledDateTime = newScheduledDateTime,
            Notes = notes,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(rescheduledOrder);

        // Act
        var result = await _orderService.RescheduleOrderAsync(orderId, newScheduledDateTime, notes);

        // Assert
        result.ScheduledDateTime.Should().BeCloseTo(newScheduledDateTime, TimeSpan.FromSeconds(1));
        result.Notes.Should().Be(notes);
        result.UpdatedAt.Should().NotBeNull();
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task RescheduleOrderAsync_WithApprovedStatus_ShouldRescheduleSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var newScheduledDateTime = DateTime.UtcNow.AddDays(2);

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var rescheduledOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved,
            ScheduledDateTime = newScheduledDateTime,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(rescheduledOrder);

        // Act
        var result = await _orderService.RescheduleOrderAsync(orderId, newScheduledDateTime);

        // Assert
        result.ScheduledDateTime.Should().BeCloseTo(newScheduledDateTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RescheduleOrderAsync_WithPastDateTime_ShouldThrowArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var pastDateTime = DateTime.UtcNow.AddHours(-1);

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1)
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderService.RescheduleOrderAsync(orderId, pastDateTime));

        // Assert
        exception.ParamName.Should().Be(nameof(pastDateTime));
        exception.Message.Should().Contain("must be in the future");
    }

    [Fact]
    public async Task RescheduleOrderAsync_WithCompletedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.RescheduleOrderAsync(orderId, DateTime.UtcNow.AddDays(1)));

        // Assert
        exception.Message.Should().Contain("Cannot reschedule order with status Completed");
    }

    [Fact]
    public async Task RescheduleOrderAsync_WithLocalDateTime_ShouldNormalizeToUtc()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var localDateTime = DateTime.Now.AddDays(1);

        var existingOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1)
        };

        var rescheduledOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ScheduledDateTime = localDateTime.ToUniversalTime(),
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(rescheduledOrder);

        // Act
        var result = await _orderService.RescheduleOrderAsync(orderId, localDateTime);

        // Assert
        result.ScheduledDateTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region DeleteOrderAsync Tests

    [Fact]
    public async Task DeleteOrderAsync_WithValidOrderId_ShouldReturnTrue()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.DeleteAsync(orderId))
            .ReturnsAsync(true);

        // Act
        var result = await _orderService.DeleteOrderAsync(orderId);

        // Assert
        result.Should().BeTrue();
        _mockOrderRepository.Verify(x => x.DeleteAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task DeleteOrderAsync_WithInvalidOrderId_ShouldReturnFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.DeleteAsync(orderId))
            .ReturnsAsync(false);

        // Act
        var result = await _orderService.DeleteOrderAsync(orderId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}