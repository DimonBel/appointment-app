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
using Moq;
using Xunit;

namespace Appointment.UnitTests.Services;

/// <summary>
/// Comprehensive unit tests for OrderApprovalService covering all scenarios and functionality
/// Module: Order Approval Module (1.3) and Order History & Audit Module (1.6)
/// </summary>
public class OrderApprovalServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IOrderHistoryRepository> _mockOrderHistoryRepository;
    private readonly Mock<IProfessionalRepository> _mockProfessionalRepository;
    private readonly Mock<IAvailabilitySlotRepository> _mockAvailabilitySlotRepository;
    private readonly OrderApprovalService _orderApprovalService;

    public OrderApprovalServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockOrderHistoryRepository = new Mock<IOrderHistoryRepository>();
        _mockProfessionalRepository = new Mock<IProfessionalRepository>();
        _mockAvailabilitySlotRepository = new Mock<IAvailabilitySlotRepository>();

        _orderApprovalService = new OrderApprovalService(
            _mockOrderRepository.Object,
            _mockOrderHistoryRepository.Object,
            _mockProfessionalRepository.Object,
            _mockAvailabilitySlotRepository.Object);
    }

    #region ApproveOrderAsync Tests

    [Fact]
    public async Task ApproveOrderAsync_WithRequestedStatus_ShouldApproveSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var professionalEntityId = Guid.NewGuid();
        const string reason = "Approved by doctor";
        var approvedByUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
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

        var approvedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved,
            ApprovalReason = reason,
            UpdatedAt = DateTime.UtcNow
        };

        var daySlots = new List<AvailabilitySlot>
        {
            new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                StartTime = order.ScheduledDateTime.TimeOfDay,
                EndTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
                IsAvailable = true
            },
            new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                StartTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
                EndTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(60)),
                IsAvailable = true
            }
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalEntityId, It.IsAny<DateTime>(), order.DurationMinutes))
            .ReturnsAsync(true);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(
            professionalEntityId, It.IsAny<DateTime>()))
            .ReturnsAsync(daySlots);
        _mockAvailabilitySlotRepository.Setup(x => x.UpdateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(approvedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        var result = await _orderApprovalService.ApproveOrderAsync(orderId, reason, approvedByUserId);

        // Assert
        result.Status.Should().Be(OrderStatus.Approved);
        result.ApprovalReason.Should().Be(reason);
        result.UpdatedAt.Should().NotBeNull();
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderHistoryRepository.Verify(x => x.CreateAsync(It.IsAny<OrderHistory>()), Times.Once);
    }

    [Fact]
    public async Task ApproveOrderAsync_WithNonExistentOrder_ShouldThrowArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderApprovalService.ApproveOrderAsync(orderId));

        // Assert
        exception.ParamName.Should().Be(nameof(orderId));
        exception.Message.Should().Contain("Order not found");
    }

    [Fact]
    public async Task ApproveOrderAsync_WithApprovedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot approve order with status Approved");
    }

    [Fact]
    public async Task ApproveOrderAsync_WithDeclinedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Declined
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot approve order with status Declined");
    }

    [Fact]
    public async Task ApproveOrderAsync_WithCompletedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot approve order with status Completed");
    }

    [Fact]
    public async Task ApproveOrderAsync_WithNonExistentProfessional_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ProfessionalId = professionalId
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync((Professional?)null);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Professional profile not found");
    }

    [Fact]
    public async Task ApproveOrderAsync_WithUnavailableSlot_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var professionalEntityId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ProfessionalId = professionalId,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var professional = new Professional
        {
            Id = professionalEntityId,
            UserId = professionalId
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalEntityId, It.IsAny<DateTime>(), order.DurationMinutes))
            .ReturnsAsync(false);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Requested time slot is not available");
    }

    [Fact]
    public async Task ApproveOrderAsync_ShouldReserveAvailableSlots()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var professionalEntityId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ProfessionalId = professionalId,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var professional = new Professional
        {
            Id = professionalEntityId,
            UserId = professionalId
        };

        var approvedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved
        };

        var availableSlots = new List<AvailabilitySlot>
        {
            new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                StartTime = order.ScheduledDateTime.TimeOfDay,
                EndTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
                IsAvailable = true
            },
            new AvailabilitySlot
            {
                Id = Guid.NewGuid(),
                StartTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
                EndTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(60)),
                IsAvailable = true
            }
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalEntityId, It.IsAny<DateTime>(), order.DurationMinutes))
            .ReturnsAsync(true);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(
            professionalEntityId, It.IsAny<DateTime>()))
            .ReturnsAsync(availableSlots);
        _mockAvailabilitySlotRepository.Setup(x => x.UpdateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(approvedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        await _orderApprovalService.ApproveOrderAsync(orderId);

        // Assert
        _mockAvailabilitySlotRepository.Verify(x => x.UpdateAsync(
            It.Is<AvailabilitySlot>(s => !s.IsAvailable)), Times.Exactly(2));
    }

    [Fact]
    public async Task ApproveOrderAsync_ShouldCreateOrderHistoryEntry()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var professionalEntityId = Guid.NewGuid();
        const string reason = "Approved";
        var approvedByUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ProfessionalId = professionalId,
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 30
        };

        var professional = new Professional
        {
            Id = professionalEntityId,
            UserId = professionalId
        };

        var approvedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved
        };

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = order.ScheduledDateTime.TimeOfDay,
            EndTime = order.ScheduledDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(30)),
            IsAvailable = true
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.IsSlotAvailableAsync(
            professionalEntityId, It.IsAny<DateTime>(), order.DurationMinutes))
            .ReturnsAsync(true);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(
            professionalEntityId, It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { slot });
        _mockAvailabilitySlotRepository.Setup(x => x.UpdateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(approvedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        await _orderApprovalService.ApproveOrderAsync(orderId, reason, approvedByUserId);

        // Assert
        _mockOrderHistoryRepository.Verify(x => x.CreateAsync(
            It.Is<OrderHistory>(h =>
                h.OrderId == orderId &&
                h.PreviousStatus == OrderStatus.Requested &&
                h.NewStatus == OrderStatus.Approved &&
                h.Reason == reason &&
                h.ChangedByUserId == approvedByUserId)), Times.Once);
    }

    #endregion

    #region DeclineOrderAsync Tests

    [Fact]
    public async Task DeclineOrderAsync_WithRequestedStatus_ShouldDeclineSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string reason = "Doctor not available";
        var declinedByUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested,
            ProfessionalId = Guid.NewGuid(),
            ScheduledDateTime = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 60
        };

        var declinedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Declined,
            DeclineReason = reason,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(declinedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        var result = await _orderApprovalService.DeclineOrderAsync(orderId, reason, declinedByUserId);

        // Assert
        result.Status.Should().Be(OrderStatus.Declined);
        result.DeclineReason.Should().Be(reason);
        result.UpdatedAt.Should().NotBeNull();
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderHistoryRepository.Verify(x => x.CreateAsync(It.IsAny<OrderHistory>()), Times.Once);
    }

    [Fact]
    public async Task DeclineOrderAsync_WithApprovedStatus_ShouldDeclineAndReleaseSlots()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var professionalId = Guid.NewGuid();
        var professionalEntityId = Guid.NewGuid();
        const string reason = "Rescheduling needed";

        var order = new Order
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
            UserId = professionalId
        };

        var slot1 = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            IsAvailable = false
        };

        var slot2 = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            StartTime = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30)),
            EndTime = TimeSpan.FromHours(11),
            IsAvailable = false
        };

        var declinedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Declined,
            DeclineReason = reason,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockProfessionalRepository.Setup(x => x.GetByUserIdAsync(professionalId))
            .ReturnsAsync(professional);
        _mockAvailabilitySlotRepository.Setup(x => x.GetSlotsByDateAsync(
            professionalEntityId, It.IsAny<DateTime>()))
            .ReturnsAsync(new[] { slot1, slot2 });
        _mockAvailabilitySlotRepository.Setup(x => x.UpdateAsync(It.IsAny<AvailabilitySlot>()))
            .ReturnsAsync((AvailabilitySlot s) => s);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(declinedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        var result = await _orderApprovalService.DeclineOrderAsync(orderId, reason);

        // Assert
        result.Status.Should().Be(OrderStatus.Declined);
        _mockAvailabilitySlotRepository.Verify(x => x.UpdateAsync(
            It.Is<AvailabilitySlot>(s => s.IsAvailable)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeclineOrderAsync_WithNonExistentOrder_ShouldThrowArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderApprovalService.DeclineOrderAsync(orderId, "Reason"));

        // Assert
        exception.ParamName.Should().Be(nameof(orderId));
        exception.Message.Should().Contain("Order not found");
    }

    [Fact]
    public async Task DeclineOrderAsync_WithCompletedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.DeclineOrderAsync(orderId, "Reason"));

        // Assert
        exception.Message.Should().Contain("Cannot decline order with status Completed");
    }

    [Fact]
    public async Task DeclineOrderAsync_WithCancelledStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Cancelled
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.DeclineOrderAsync(orderId, "Reason"));

        // Assert
        exception.Message.Should().Contain("Cannot decline order with status Cancelled");
    }

    [Fact]
    public async Task DeclineOrderAsync_WithNoShowStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.NoShow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.DeclineOrderAsync(orderId, "Reason"));

        // Assert
        exception.Message.Should().Contain("Cannot decline order with status NoShow");
    }

    #endregion

    #region CompleteOrderAsync Tests

    [Fact]
    public async Task CompleteOrderAsync_WithApprovedStatus_ShouldCompleteSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string notes = "Appointment completed successfully";
        var completedByUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved
        };

        var completedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            Notes = notes,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(completedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        var result = await _orderApprovalService.CompleteOrderAsync(orderId, notes, completedByUserId);

        // Assert
        result.Status.Should().Be(OrderStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
        result.Notes.Should().Be(notes);
        result.UpdatedAt.Should().NotBeNull();
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderHistoryRepository.Verify(x => x.CreateAsync(It.IsAny<OrderHistory>()), Times.Once);
    }

    [Fact]
    public async Task CompleteOrderAsync_WithRequestedStatus_ShouldCompleteSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested
        };

        var completedOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed,
            CompletedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(completedOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        var result = await _orderApprovalService.CompleteOrderAsync(orderId);

        // Assert
        result.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task CompleteOrderAsync_WithDeclinedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Declined
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.CompleteOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot complete order with status Declined");
    }

    [Fact]
    public async Task CompleteOrderAsync_WithCancelledStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Cancelled
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.CompleteOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot complete order with status Cancelled");
    }

    [Fact]
    public async Task CompleteOrderAsync_WithNoShowStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.NoShow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.CompleteOrderAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot complete order with status NoShow");
    }

    [Fact]
    public async Task CompleteOrderAsync_WithNonExistentOrder_ShouldThrowArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderApprovalService.CompleteOrderAsync(orderId));

        // Assert
        exception.ParamName.Should().Be(nameof(orderId));
        exception.Message.Should().Contain("Order not found");
    }

    #endregion

    #region MarkAsNoShowAsync Tests

    [Fact]
    public async Task MarkAsNoShowAsync_WithApprovedStatus_ShouldMarkSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        const string notes = "Client did not show up";
        var markedByUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved
        };

        var noShowOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.NoShow,
            Notes = notes,
            UpdatedAt = DateTime.UtcNow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(noShowOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        var result = await _orderApprovalService.MarkAsNoShowAsync(orderId, notes, markedByUserId);

        // Assert
        result.Status.Should().Be(OrderStatus.NoShow);
        result.Notes.Should().Be(notes);
        result.UpdatedAt.Should().NotBeNull();
        _mockOrderRepository.Verify(x => x.UpdateAsync(It.IsAny<Order>()), Times.Once);
        _mockOrderHistoryRepository.Verify(x => x.CreateAsync(It.IsAny<OrderHistory>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsNoShowAsync_WithRequestedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Requested
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.MarkAsNoShowAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot mark order as no-show with status Requested");
    }

    [Fact]
    public async Task MarkAsNoShowAsync_WithDeclinedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Declined
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.MarkAsNoShowAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot mark order as no-show with status Declined");
    }

    [Fact]
    public async Task MarkAsNoShowAsync_WithCancelledStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Cancelled
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.MarkAsNoShowAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot mark order as no-show with status Cancelled");
    }

    [Fact]
    public async Task MarkAsNoShowAsync_WithCompletedStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Completed
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.MarkAsNoShowAsync(orderId));

        // Assert
        exception.Message.Should().Contain("Cannot mark order as no-show with status Completed");
    }

    [Fact]
    public async Task MarkAsNoShowAsync_WithNonExistentOrder_ShouldThrowArgumentException()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderApprovalService.MarkAsNoShowAsync(orderId));

        // Assert
        exception.ParamName.Should().Be(nameof(orderId));
        exception.Message.Should().Contain("Order not found");
    }

    [Fact]
    public async Task MarkAsNoShowAsync_ShouldCreateOrderHistoryEntryWithCorrectReason()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var markedByUserId = Guid.NewGuid();

        var order = new Order
        {
            Id = orderId,
            Status = OrderStatus.Approved
        };

        var noShowOrder = new Order
        {
            Id = orderId,
            Status = OrderStatus.NoShow
        };

        _mockOrderRepository.Setup(x => x.GetByIdAsync(orderId))
            .ReturnsAsync(order);
        _mockOrderRepository.Setup(x => x.UpdateAsync(It.IsAny<Order>()))
            .ReturnsAsync(noShowOrder);
        _mockOrderHistoryRepository.Setup(x => x.CreateAsync(It.IsAny<OrderHistory>()))
            .ReturnsAsync((OrderHistory h) => h);

        // Act
        await _orderApprovalService.MarkAsNoShowAsync(orderId, markedByUserId: markedByUserId);

        // Assert
        _mockOrderHistoryRepository.Verify(x => x.CreateAsync(
            It.Is<OrderHistory>(h =>
                h.OrderId == orderId &&
                h.PreviousStatus == OrderStatus.Approved &&
                h.NewStatus == OrderStatus.NoShow &&
                h.Reason == "No-show" &&
                h.ChangedByUserId == markedByUserId)), Times.Once);
    }

    #endregion

    #region GetOrderHistoryAsync Tests

    [Fact]
    public async Task GetOrderHistoryAsync_WithValidOrderId_ShouldReturnHistory()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var history = new List<OrderHistory>
        {
            new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PreviousStatus = OrderStatus.Requested,
                NewStatus = OrderStatus.Approved,
                ChangedAt = DateTime.UtcNow.AddHours(-2)
            },
            new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PreviousStatus = OrderStatus.Approved,
                NewStatus = OrderStatus.Completed,
                ChangedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _mockOrderHistoryRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(history);

        // Act
        var result = await _orderApprovalService.GetOrderHistoryAsync(orderId);

        // Assert
        result.Should().HaveCount(2);
        result.All(h => h.OrderId == orderId).Should().BeTrue();
        _mockOrderHistoryRepository.Verify(x => x.GetByOrderIdAsync(orderId), Times.Once);
    }

    [Fact]
    public async Task GetOrderHistoryAsync_WithNoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _mockOrderHistoryRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(new List<OrderHistory>());

        // Act
        var result = await _orderApprovalService.GetOrderHistoryAsync(orderId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderHistoryAsync_ShouldReturnInChronologicalOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var history = new List<OrderHistory>
        {
            new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PreviousStatus = OrderStatus.Requested,
                NewStatus = OrderStatus.Approved,
                ChangedAt = DateTime.UtcNow.AddHours(-2)
            },
            new OrderHistory
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                PreviousStatus = OrderStatus.Approved,
                NewStatus = OrderStatus.Completed,
                ChangedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _mockOrderHistoryRepository.Setup(x => x.GetByOrderIdAsync(orderId))
            .ReturnsAsync(history);

        // Act
        var result = await _orderApprovalService.GetOrderHistoryAsync(orderId);

        // Assert
        result.Should().BeInAscendingOrder(h => h.ChangedAt);
    }

    #endregion
}