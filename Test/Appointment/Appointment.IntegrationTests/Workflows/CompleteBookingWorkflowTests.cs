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

namespace Appointment.IntegrationTests.Workflows;

/// <summary>
/// End-to-end workflow tests for complete booking scenarios
/// Tests realistic user journeys from start to finish
/// </summary>
[Collection("TestDatabase")]
public class CompleteBookingWorkflowTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IOrderService _orderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IProfessionalService _professionalService;
    private readonly IDomainConfigurationService _domainConfigurationService;
    private readonly IPreOrderDataService _preOrderDataService;
    private readonly IOrderHistoryRepository _orderHistoryRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;

    private AppIdentityUser? _clientUser;
    private AppIdentityUser? _professionalUser;
    private Professional? _professional;
    private DomainConfiguration? _domainConfiguration;

    public CompleteBookingWorkflowTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();

        _orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        _orderApprovalService = scope.ServiceProvider.GetRequiredService<IOrderApprovalService>();
        _availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
        _professionalService = scope.ServiceProvider.GetRequiredService<IProfessionalService>();
        _domainConfigurationService = scope.ServiceProvider.GetRequiredService<IDomainConfigurationService>();
        _preOrderDataService = scope.ServiceProvider.GetRequiredService<IPreOrderDataService>();
        _orderHistoryRepository = scope.ServiceProvider.GetRequiredService<IOrderHistoryRepository>();
        _availabilitySlotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Setup test users and professional
        _clientUser = await _fixture.CreateTestUserAsync("client@test.com", "John", "Doe");
        _professionalUser = await _fixture.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        _professional = await _fixture.CreateTestProfessionalAsync(
            _professionalUser, "Dr.", "General Medicine", "MD", "Family Medicine");
        _domainConfiguration = await _fixture.CreateTestDomainConfigurationAsync(
            DomainType.Medical, "General Medical Consultation", "Standard medical consultation");
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Standard Booking Workflow

    [Fact]
    public async Task StandardBookingWorkflow_ShouldCompleteSuccessfully()
    {
        // Arrange - Professional sets up availability
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        // Act - Client books appointment
        var scheduledDateTime = nextMonday.Add(TimeSpan.FromHours(10));
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60,
            "General Consultation",
            "Patient has persistent headaches",
            _domainConfiguration!.Id);

        // Assert - Order created
        order.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Requested);
        order.ClientId.Should().Be(_clientUser.Id);
        order.ProfessionalId.Should().Be(_professionalUser.Id);
        order.DomainConfigurationId.Should().Be(_domainConfiguration.Id);

        // Verify slot is reserved (even for Requested orders)
        var slots = await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, scheduledDateTime.Date);
        var bookedSlots = slots.Where(s => !s.IsAvailable).ToList();
        bookedSlots.Should().HaveCount(2); // 60 minutes = 2 slots
    }

    #endregion

    #region Booking with Pre-Order Data Workflow

    [Fact]
    public async Task BookingWithPreOrderDataWorkflow_ShouldCollectAndValidateData()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextTuesday);

        var scheduledDateTime = nextTuesday.Add(TimeSpan.FromHours(14));

        // Act - Step 1: Create order
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            45,
            "Specialist Consultation",
            "Follow-up appointment");

        // Act - Step 2: Collect pre-order data
        var preOrderFields = new System.Collections.Generic.Dictionary<string, string>
        {
            { "symptoms", "Recurring abdominal pain" },
            { "duration", "3 months" },
            { "previous_treatment", "Over-the-counter medication" },
            { "allergies", "None known" }
        };

        var preOrderData = await _preOrderDataService.CreatePreOrderDataAsync(
            order.Id, _clientUser.Id, preOrderFields);

        // Act - Step 3: Validate required fields
        var requiredFields = new System.Collections.Generic.Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" },
            { "allergies", "" }
        };

        var isValid = await _preOrderDataService.ValidatePreOrderDataAsync(
            preOrderData.Id, requiredFields);

        // Act - Step 4: Mark as complete
        await _preOrderDataService.MarkAsCompletedAsync(preOrderData.Id);

        // Act - Step 5: Approve order
        await _orderApprovalService.ApproveOrderAsync(order.Id, "Patient data collected and validated");

        // Assert
        order = await _orderService.GetOrderByIdAsync(order.Id);
        order!.Status.Should().Be(OrderStatus.Approved);

        var completedPreOrderData = await _preOrderDataService.GetPreOrderDataByOrderIdAsync(order.Id);
        completedPreOrderData!.IsCompleted.Should().BeTrue();
        completedPreOrderData.DataFields.Should().HaveCount(4);

        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(1);
    }

    #endregion

    #region Multi-Day Booking Workflow

    [Fact]
    public async Task MultiDayBookingWorkflow_ShouldManageMultipleAppointments()
    {
        // Arrange
        await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _availabilityService.CreateAvailabilityAsync(
            _professional.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);

        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextWednesday);

        // Act - Book multiple appointments
        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30,
            "Initial Consultation");

        var order2 = await _orderService.CreateOrderAsync(
            _clientUser.Id,
            _professional.Id,
            nextWednesday.Add(TimeSpan.FromHours(14)),
            30,
            "Follow-up");

        // Assert
        order1.Should().NotBeNull();
        order2.Should().NotBeNull();
        order1.Id.Should().NotBe(order2.Id);

        var clientOrders = await _orderService.GetOrdersByClientAsync(_clientUser.Id);
        clientOrders.Should().HaveCount(2);
    }

    #endregion

    #region Booking with Rescheduling Workflow

    [Fact]
    public async Task BookingWithReschedulingWorkflow_ShouldUpdateDateTime()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        var originalDateTime = nextMonday.Add(TimeSpan.FromHours(10));

        // Act - Step 1: Create order
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            originalDateTime,
            30);

        // Act - Step 2: Reschedule
        var newDateTime = nextMonday.Add(TimeSpan.FromHours(15));
        var rescheduledOrder = await _orderService.RescheduleOrderAsync(
            order.Id, newDateTime, "Rescheduled by client request");

        // Assert
        rescheduledOrder.ScheduledDateTime.Should().BeCloseTo(newDateTime, TimeSpan.FromSeconds(1));
        rescheduledOrder.Notes.Should().Be("Rescheduled by client request");

        // Verify persistence
        var retrievedOrder = await _orderService.GetOrderByIdAsync(order.Id);
        retrievedOrder!.ScheduledDateTime.Should().BeCloseTo(newDateTime, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Booking with Cancellation Workflow

    [Fact]
    public async Task BookingWithCancellationWorkflow_ShouldUpdateStatus()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        var scheduledDateTime = nextMonday.Add(TimeSpan.FromHours(10));

        // Act - Step 1: Create order
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            30);

        // Act - Step 2: Cancel order
        var cancelledOrder = await _orderService.CancelOrderAsync(
            order.Id, "Client cancelled due to emergency");

        // Assert
        cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
        cancelledOrder.Notes.Should().Be("Client cancelled due to emergency");

        // Verify persistence
        var retrievedOrder = await _orderService.GetOrderByIdAsync(order.Id);
        retrievedOrder!.Status.Should().Be(OrderStatus.Cancelled);
    }

    #endregion

    #region Full Appointment Lifecycle Workflow

    [Fact]
    public async Task FullAppointmentLifecycleWorkflow_ShouldTrackAllStages()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextTuesday);

        var scheduledDateTime = nextTuesday.Add(TimeSpan.FromHours(10));

        // Stage 1: Client books appointment
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            60,
            "Complete Checkup",
            "Annual physical examination");

        order.Status.Should().Be(OrderStatus.Requested);

        // Stage 2: Professional approves
        await _orderApprovalService.ApproveOrderAsync(order.Id, "Approved for consultation");
        order = await _orderService.GetOrderByIdAsync(order.Id);
        order!.Status.Should().Be(OrderStatus.Approved);

        // Stage 3: Appointment completed
        await _orderApprovalService.CompleteOrderAsync(order.Id, "Consultation completed successfully");
        order = await _orderService.GetOrderByIdAsync(order.Id);
        order!.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();

        // Verify complete history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(2);
        history[0].NewStatus.Should().Be(OrderStatus.Approved);
        history[1].NewStatus.Should().Be(OrderStatus.Completed);
    }

    #endregion

    #region Booking with Decline Workflow

    [Fact]
    public async Task BookingWithDeclineWorkflow_ShouldHandleRejection()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        var scheduledDateTime = nextMonday.Add(TimeSpan.FromHours(10));

        // Act - Step 1: Create order
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            30);

        // Act - Step 2: Professional declines
        var declinedOrder = await _orderApprovalService.DeclineOrderAsync(
            order.Id, "Professional not available on this date");

        // Assert
        declinedOrder.Status.Should().Be(OrderStatus.Declined);
        declinedOrder.DeclineReason.Should().Be("Professional not available on this date");

        // Verify history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(1);
        history.First().NewStatus.Should().Be(OrderStatus.Declined);
    }

    #endregion

    #region Booking with No-Show Workflow

    [Fact]
    public async Task BookingWithNoShowWorkflow_ShouldMarkOrderAsNoShow()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextWednesday);

        var scheduledDateTime = nextWednesday.Add(TimeSpan.FromHours(11));

        // Act - Step 1: Create and approve order
        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            30);

        await _orderApprovalService.ApproveOrderAsync(order.Id);

        // Act - Step 2: Mark as no-show
        var noShowOrder = await _orderApprovalService.MarkAsNoShowAsync(
            order.Id, "Client did not attend the appointment");

        // Assert
        noShowOrder.Status.Should().Be(OrderStatus.NoShow);
        noShowOrder.Notes.Should().Be("Client did not attend the appointment");

        // Verify history
        var history = await _orderHistoryRepository.GetByOrderIdAsync(order.Id);
        history.Should().HaveCount(2);
        history.Should().Contain(h => h.NewStatus == OrderStatus.NoShow);
    }

    #endregion

    #region Multiple Clients Booking Workflow

    [Fact]
    public async Task MultipleClientsBookingWorkflow_ShouldManageConcurrentBookings()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        var client2 = await _fixture.CreateTestUserAsync("client2@test.com", "Alice", "Johnson");
        var client3 = await _fixture.CreateTestUserAsync("client3@test.com", "Bob", "Smith");

        // Act - Multiple clients book
        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            nextMonday.Add(TimeSpan.FromHours(9)),
            30);

        var order2 = await _orderService.CreateOrderAsync(
            client2.Id,
            _professional.Id,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        var order3 = await _orderService.CreateOrderAsync(
            client3.Id,
            _professional.Id,
            nextMonday.Add(TimeSpan.FromHours(11)),
            30);

        // Assert
        var professionalOrders = await _orderService.GetOrdersByProfessionalAsync(_professionalUser!.Id);
        professionalOrders.Should().HaveCount(3);

        var clients = await _orderService.GetClientsByProfessionalAsync(_professionalUser.Id);
        clients.Should().HaveCount(3);
    }

    #endregion

    #region Booking with Domain Configuration Workflow

    [Fact]
    public async Task BookingWithDifferentDomainConfigurationsWorkflow_ShouldSupportMultipleTypes()
    {
        // Arrange
        var medicalConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Medical, "Medical Consultation", "Medical services", 30);

        var legalConfig = await _domainConfigurationService.CreateDomainConfigurationAsync(
            DomainType.Legal, "Legal Consultation", "Legal services", 60);

        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        // Act - Book with different domain configurations
        var medicalOrder = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            nextMonday.Add(TimeSpan.FromHours(9)),
            30,
            domainConfigurationId: medicalConfig.Id);

        var legalOrder = await _orderService.CreateOrderAsync(
            _clientUser.Id,
            _professional.Id,
            nextMonday.Add(TimeSpan.FromHours(10)),
            60,
            domainConfigurationId: legalConfig.Id);

        // Assert
        medicalOrder.DomainConfigurationId.Should().Be(medicalConfig.Id);
        legalOrder.DomainConfigurationId.Should().Be(legalConfig.Id);

        var retrievedMedicalOrder = await _orderService.GetOrderByIdAsync(medicalOrder.Id);
        var retrievedLegalOrder = await _orderService.GetOrderByIdAsync(legalOrder.Id);

        retrievedMedicalOrder!.DomainConfigurationId.Should().Be(medicalConfig.Id);
        retrievedLegalOrder!.DomainConfigurationId.Should().Be(legalConfig.Id);
    }

    #endregion

    #region Booking with Professional Availability Toggle Workflow

    [Fact]
    public async Task BookingWithProfessionalAvailabilityToggleWorkflow_ShouldRespectAvailability()
    {
        // Arrange
        await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextMonday);

        // Act - Step 1: Disable professional availability
        await _professionalService.SetProfessionalAvailabilityAsync(_professional.Id, false);

        // Act & Assert - Step 2: Try to book - should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(
                _clientUser!.Id,
                _professional!.Id,
                nextMonday.Add(TimeSpan.FromHours(10)),
                30));

        // Act - Step 3: Re-enable availability
        await _professionalService.SetProfessionalAvailabilityAsync(_professional.Id, true);

        // Act - Step 4: Try to book - should succeed
        var order = await _orderService.CreateOrderAsync(
            _clientUser.Id,
            _professional.Id,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        order.Should().NotBeNull();
    }

    #endregion

    #region Booking with Time Slot Conflict Workflow

    [Fact]
    public async Task BookingWithTimeSlotConflictWorkflow_ShouldPreventDoubleBooking()
    {
        // Arrange
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, nextTuesday);

        var scheduledDateTime = nextTuesday.Add(TimeSpan.FromHours(10));

        // Act - Step 1: First booking
        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id,
            _professional!.Id,
            scheduledDateTime,
            30);

        // Act & Assert - Step 2: Try to book same slot - should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(
                _clientUser.Id,
                _professional.Id,
                scheduledDateTime,
                30));
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