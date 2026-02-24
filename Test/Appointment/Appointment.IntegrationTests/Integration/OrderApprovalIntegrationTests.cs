using System;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using Appointment.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Appointment.IntegrationTests.Integration;

/// <summary>
/// Integration tests for Order Approval Module (1.3) and Order History & Audit Module (1.6)
/// Tests end-to-end workflows with real database operations including history tracking
/// </summary>
[Collection("TestDatabase")]
public class OrderApprovalIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IOrderService _orderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IOrderHistoryRepository _orderHistoryRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly IAvailabilityService _availabilityService;

    private AppIdentityUser? _clientUser;
    private AppIdentityUser? _professionalUser;
    private Professional? _professional;
    private Availability? _availability;

    public OrderApprovalIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();

        _orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        _orderApprovalService = scope.ServiceProvider.GetRequiredService<IOrderApprovalService>();
        _orderHistoryRepository = scope.ServiceProvider.GetRequiredService<IOrderHistoryRepository>();
        _professionalRepository = scope.ServiceProvider.GetRequiredService<IProfessionalRepository>();
        _availabilitySlotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        _availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Setup test data
        _clientUser = await _fixture.CreateTestUserAsync("client@test.com", "John", "Doe");
        _professionalUser = await _fixture.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        _professional = await _fixture.CreateTestProfessionalAsync(_professionalUser, "Dr.", "General Medicine");
        _availability = await _fixture.CreateTestAvailabilityAsync(
            _professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Generate slots for next Monday
        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Complete Order Approval Workflow

    [Fact]
    public async Task CompleteOrderApprovalWorkflow_ShouldUpdateStatusAndCreateHistory()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        const string approvalReason = "Approved by Dr. Smith";
        var approvedByUserId = _professionalUser!.Id;

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60,
            "General Consultation");

        // Act
        var approvedOrder = await _orderApprovalService.ApproveOrderAsync(
            order.Id, approvalReason, approvedByUserId);

        // Assert - Order status updated
        approvedOrder.Status.Should().Be(OrderStatus.Approved);
        approvedOrder.ApprovalReason.Should().Be(approvalReason);
        approvedOrder.UpdatedAt.Should().NotBeNull();

        // Verify slots were reserved
        var slots = await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, scheduledDateTime.Date);
        var bookedSlots = slots.Where(s => !s.IsAvailable).ToList();
        bookedSlots.Should().HaveCount(2); // 60 minutes = 2 slots

        // Verify order history was created
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(1);
        history.First().PreviousStatus.Should().Be(OrderStatus.Requested);
        history.First().NewStatus.Should().Be(OrderStatus.Approved);
        history.First().Reason.Should().Be(approvalReason);
        history.First().ChangedByUserId.Should().Be(approvedUserId);
    }

    [Fact]
    public async Task ApproveOrderWithoutAvailability_ShouldThrowException()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Book all slots for that time
        var slots = await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, scheduledDateTime.Date);
        foreach (var slot in slots.Where(s => !s.IsAvailable))
        {
            slot.IsAvailable = false;
            await _availabilitySlotRepository.UpdateAsync(slot);
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(order.Id));
    }

    #endregion

    #region Complete Order Decline Workflow

    [Fact]
    public async Task CompleteOrderDeclineWorkflow_ShouldUpdateStatusAndCreateHistory()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        const string declineReason = "Doctor not available on this date";
        var declinedByUserId = _professionalUser!.Id;

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Act
        var declinedOrder = await _orderApprovalService.DeclineOrderAsync(
            order.Id, declineReason, declinedByUserId);

        // Assert - Order status updated
        declinedOrder.Status.Should().Be(OrderStatus.Declined);
        declinedOrder.DeclineReason.Should().Be(declineReason);
        declinedOrder.UpdatedAt.Should().NotBeNull();

        // Verify order history was created
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(1);
        history.First().PreviousStatus.Should().Be(OrderStatus.Requested);
        history.First().NewStatus.Should().Be(OrderStatus.Declined);
        history.First().Reason.Should().Be(declineReason);
    }

    [Fact]
    public async Task DeclineApprovedOrderWorkflow_ShouldReleaseSlotsAndUpdateHistory()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        const string declineReason = "Emergency rescheduling required";

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // First approve the order
        await _orderApprovalService.ApproveOrderAsync(order.Id);

        // Get booked slots
        var slotsBeforeDecline = await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, scheduledDateTime.Date);
        var bookedSlotsBeforeDecline = slotsBeforeDecline.Where(s => !s.IsAvailable).ToList();

        // Act
        var declinedOrder = await _orderApprovalService.DeclineOrderAsync(order.Id, declineReason);

        // Assert
        declinedOrder.Status.Should().Be(OrderStatus.Declined);

        // Verify slots were released
        var slotsAfterDecline = await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, scheduledDateTime.Date);
        var bookedSlotsAfterDecline = slotsAfterDecline.Where(s => !s.IsAvailable).ToList();
        bookedSlotsAfterDecline.Should().HaveCountLessThan(bookedSlotsBeforeDecline.Count);

        // Verify order history - should have 2 entries (approve and decline)
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(2);
        history.Should().Contain(h => h.NewStatus == OrderStatus.Approved);
        history.Should().Contain(h => h.NewStatus == OrderStatus.Declined);
    }

    #endregion

    #region Complete Order Completion Workflow

    [Fact]
    public async Task CompleteOrderWorkflow_ShouldUpdateStatusAndCreateHistory()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        const string completionNotes = "Appointment completed successfully";
        var completedByUserId = _professionalUser!.Id;

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // First approve the order
        await _orderApprovalService.ApproveOrderAsync(order.Id);

        // Act
        var completedOrder = await _orderApprovalService.CompleteOrderAsync(
            order.Id, completionNotes, completedByUserId);

        // Assert
        completedOrder.Status.Should().Be(OrderStatus.Completed);
        completedOrder.CompletedAt.Should().NotBeNull();
        completedOrder.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        completedOrder.Notes.Should().Be(completionNotes);

        // Verify order history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(2); // Approve and Complete
        history.Should().Contain(h => h.NewStatus == OrderStatus.Completed && h.Notes == completionNotes);
    }

    [Fact]
    public async Task CompleteOrderFromRequestedStatus_ShouldWork()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Act - Complete directly from Requested status
        var completedOrder = await _orderApprovalService.CompleteOrderAsync(order.Id);

        // Assert
        completedOrder.Status.Should().Be(OrderStatus.Completed);
        completedOrder.CompletedAt.Should().NotBeNull();

        // Verify order history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(1);
        history.First().PreviousStatus.Should().Be(OrderStatus.Requested);
        history.First().NewStatus.Should().Be(OrderStatus.Completed);
    }

    #endregion

    #region Complete No-Show Workflow

    [Fact]
    public async Task MarkAsNoShowWorkflow_ShouldUpdateStatusAndCreateHistory()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        const string noShowNotes = "Client did not show up for appointment";
        var markedByUserId = _professionalUser!.Id;

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // First approve the order
        await _orderApprovalService.ApproveOrderAsync(order.Id);

        // Act
        var noShowOrder = await _orderApprovalService.MarkAsNoShowAsync(
            order.Id, noShowNotes, markedByUserId);

        // Assert
        noShowOrder.Status.Should().Be(OrderStatus.NoShow);
        noShowOrder.Notes.Should().Be(noShowNotes);

        // Verify order history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(2);
        history.Should().Contain(h => h.NewStatus == OrderStatus.NoShow && h.Reason == "No-show");
    }

    [Fact]
    public async Task MarkAsNoShowFromNonApprovedStatus_ShouldFail()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Act & Assert - Try to mark as no-show from Requested status
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.MarkAsNoShowAsync(order.Id));
    }

    #endregion

    #region Full Order Lifecycle Workflow

    [Fact]
    public async Task CompleteOrderLifecycle_ShouldTrackAllTransitions()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var approvedByUserId = _professionalUser!.Id;
        var completedByUserId = _professionalUser!.Id;

        // Step 1: Create order
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60,
            "Full Lifecycle Test");

        order.Status.Should().Be(OrderStatus.Requested);

        // Step 2: Approve order
        var approvedOrder = await _orderApprovalService.ApproveOrderAsync(
            order.Id, "Initial approval", approvedByUserId);

        approvedOrder.Status.Should().Be(OrderStatus.Approved);

        // Step 3: Complete order
        var completedOrder = await _orderApprovalService.CompleteOrderAsync(
            order.Id, "Service completed", completedByUserId);

        completedOrder.Status.Should().Be(OrderStatus.Completed);
        completedOrder.CompletedAt.Should().NotBeNull();

        // Verify full history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(2);
        
        // Verify sequence
        history[0].PreviousStatus.Should().Be(OrderStatus.Requested);
        history[0].NewStatus.Should().Be(OrderStatus.Approved);
        history[1].PreviousStatus.Should().Be(OrderStatus.Approved);
        history[1].NewStatus.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task DeclineThenResubmitWorkflow_ShouldTrackAllTransitions()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var newScheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(14));

        // Step 1: Create and decline order
        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        await _orderApprovalService.DeclineOrderAsync(order1.Id, "Time conflict");

        // Step 2: Create new order (simulating resubmission)
        var order2 = await _orderService.CreateOrderAsync(
            _clientUser.Id,
            _professional.Id,
            newScheduledDateTime,
            60);

        // Step 3: Approve new order
        await _orderApprovalService.ApproveOrderAsync(order2.Id, "Approved for new time");

        // Verify history for both orders
        var history1 = await _orderHistoryRepository.GetByOrderIdAsync(order1.Id);
        history1.Should().HaveCount(1);
        history1.First().NewStatus.Should().Be(OrderStatus.Declined);

        var history2 = await _orderHistoryRepository.GetByOrderIdAsync(order2.Id);
        history2.Should().HaveCount(1);
        history2.First().NewStatus.Should().Be(OrderStatus.Approved);
    }

    #endregion

    #region Order History Retrieval Workflow

    [Fact]
    public async Task GetOrderHistoryWorkflow_ShouldReturnCompleteAuditTrail()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Create multiple status changes
        await _orderApprovalService.ApproveOrderAsync(order.Id, "Approved");
        await Task.Delay(100); // Small delay to ensure different timestamps
        await _orderApprovalService.CompleteOrderAsync(order.Id, "Completed");

        // Act
        var history = await _orderApprovalService.GetOrderHistoryAsync(order.Id);

        // Assert
        history.Should().HaveCount(2);
        history.Should().BeInAscendingOrder(h => h.ChangedAt);
        
        // Verify sequence
        history[0].PreviousStatus.Should().Be(OrderStatus.Requested);
        history[0].NewStatus.Should().Be(OrderStatus.Approved);
        history[1].PreviousStatus.Should().Be(OrderStatus.Approved);
        history[1].NewStatus.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task GetOrderHistoryForNewOrder_ShouldReturnEmpty()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Act
        var history = await _orderApprovalService.GetOrderHistoryAsync(order.Id);

        // Assert
        history.Should().BeEmpty();
    }

    #endregion

    #region Invalid State Transition Workflows

    [Fact]
    public async Task AttemptToApproveCompletedOrder_ShouldFail()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        await _orderApprovalService.ApproveOrderAsync(order.Id);
        await _orderApprovalService.CompleteOrderAsync(order.Id);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.ApproveOrderAsync(order.Id));
    }

    [Fact]
    public async Task AttemptToCompleteDeclinedOrder_ShouldFail()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        await _orderApprovalService.DeclineOrderAsync(order.Id, "Not available");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderApprovalService.CompleteOrderAsync(order.Id));
    }

    #endregion

    #region Multiple Orders Approval Workflow

    [Fact]
    public async Task ApproveMultipleOrders_ShouldManageSlotsCorrectly()
    {
        // Arrange
        var scheduledDateTime1 = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        var scheduledDateTime2 = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(11));

        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime1,
            30);

        var order2 = await _orderService.CreateOrderAsync(
            _clientUser.Id,
            _professional.Id,
            scheduledDateTime2,
            30);

        // Act
        await _orderApprovalService.ApproveOrderAsync(order1.Id);
        await _orderApprovalService.ApproveOrderAsync(order2.Id);

        // Assert
        var approvedOrder1 = await _orderService.GetOrderByIdAsync(order1.Id);
        var approvedOrder2 = await _orderService.GetOrderByIdAsync(order2.Id);

        approvedOrder1!.Status.Should().Be(OrderStatus.Approved);
        approvedOrder2!.Status.Should().Be(OrderStatus.Approved);

        // Verify different slots were booked
        var history1 = await _orderHistoryRepository.GetByOrderIdAsync(order1.Id);
        var history2 = await _orderHistoryRepository.GetByOrderIdAsync(order2.Id);

        history1.Should().HaveCount(1);
        history2.Should().HaveCount(1);
    }

    #endregion

    #region Approval With Reason Workflow

    [Fact]
    public async Task ApproveOrderWithReason_ShouldStoreReasonInHistory()
    {
        // Arrange
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).Add(TimeSpan.FromHours(10));
        const string detailedReason = "Approved after reviewing patient medical history. No contraindications found.";

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60);

        // Act
        await _orderApprovalService.ApproveOrderAsync(order.Id, detailedReason);

        // Assert
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.First().Reason.Should().Be(detailedReason);
    }

    #endregion

    #region Helper Methods

    private DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow;
        var daysUntilDay = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysUntilDay);
        return nextDay.Date;
    }

    #endregion
}