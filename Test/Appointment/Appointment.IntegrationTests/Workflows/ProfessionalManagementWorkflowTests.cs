using System;
using System.Linq;
using System.Threading.Tasks;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Service.Services;
using Appointment.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Appointment.IntegrationTests.Workflows;

/// <summary>
/// End-to-end workflow tests for professional management scenarios
/// Tests professional lifecycle, availability management, and booking operations
/// </summary>
[Collection("TestDatabase")]
public class ProfessionalManagementWorkflowTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IProfessionalService _professionalService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IOrderService _orderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;

    private AppIdentityUser? _professionalUser;
    private AppIdentityUser? _clientUser;

    public ProfessionalManagementWorkflowTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();

        _professionalService = scope.ServiceProvider.GetRequiredService<IProfessionalService>();
        _availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
        _orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        _orderApprovalService = scope.ServiceProvider.GetRequiredService<IOrderApprovalService>();
        _professionalRepository = scope.ServiceProvider.GetRequiredService<IProfessionalRepository>();
        _availabilitySlotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        // Setup test users
        _professionalUser = await _fixture.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        _clientUser = await _fixture.CreateTestUserAsync("client@test.com", "John", "Doe");
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Complete Professional Onboarding Workflow

    [Fact]
    public async Task CompleteProfessionalOnboardingWorkflow_ShouldCreateProfileAndAvailability()
    {
        // Act - Step 1: Create professional profile
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.",
            qualifications: "MD, Board Certified",
            specialization: "Family Medicine");

        // Assert
        professional.Should().NotBeNull();
        professional.UserId.Should().Be(_professionalUser.Id);
        professional.Title.Should().Be("Dr.");
        professional.Qualifications.Should().Be("MD, Board Certified");
        professional.Specialization.Should().Be("Family Medicine");
        professional.IsAvailable.Should().BeTrue();
        professional.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Act - Step 2: Set up weekly availability
        var availability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17),
            ScheduleType.Recurring);

        // Assert
        availability.Should().NotBeNull();
        availability.ProfessionalId.Should().Be(professional.Id);
        availability.DayOfWeek.Should().Be(DayOfWeek.Monday);
        availability.IsActive.Should().BeTrue();

        // Act - Step 3: Generate slots for next week
        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        var slots = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);

        // Assert
        slots.Should().HaveCount(16); // 8 hours = 16 slots
        slots.All(s => s.AvailabilityId == availability.Id).Should().BeTrue();
    }

    #endregion

    #region Professional Profile Update Workflow

    [Fact]
    public async Task ProfessionalProfileUpdateWorkflow_ShouldPersistAllChanges()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.",
            qualifications: "MD");

        // Act - Update multiple fields
        var updatedProfessional = await _professionalService.UpdateProfessionalAsync(
            professional.Id,
            title: "Dr.",
            qualifications: "MD, PhD",
            specialization: "Internal Medicine",
            hourlyRate: 150,
            experienceYears: 10,
            bio: "Experienced physician specializing in internal medicine");

        // Assert
        updatedProfessional.Qualifications.Should().Be("MD, PhD");
        updatedProfessional.Specialization.Should().Be("Internal Medicine");
        updatedProfessional.HourlyRate.Should().Be(150);
        updatedProfessional.ExperienceYears.Should().Be(10);
        updatedProfessional.Bio.Should().Be("Experienced physician specializing in internal medicine");
        updatedProfessional.UpdatedAt.Should().NotBeNull();

        // Verify persistence
        var retrievedProfessional = await _professionalRepository.GetByIdAsync(professional.Id);
        retrievedProfessional!.Qualifications.Should().Be("MD, PhD");
        retrievedProfessional.Specialization.Should().Be("Internal Medicine");
    }

    #endregion

    #region Professional Availability Management Workflow

    [Fact]
    public async Task ProfessionalAvailabilityManagementWorkflow_ShouldSupportMultipleDays()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        // Act - Create availability for multiple days
        var mondayAvailability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var wednesdayAvailability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(13));

        var fridayAvailability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Friday, TimeSpan.FromHours(14), TimeSpan.FromHours(18));

        // Assert
        var availabilities = await _availabilityService.GetAvailabilitiesByProfessionalAsync(professional.Id);
        availabilities.Should().HaveCount(3);

        // Generate slots for each day
        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        var nextFriday = GetNextDayOfWeek(DayOfWeek.Friday);

        var mondaySlots = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);
        var wednesdaySlots = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextWednesday);
        var fridaySlots = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextFriday);

        mondaySlots.Should().HaveCount(16); // 8 hours
        wednesdaySlots.Should().HaveCount(8); // 4 hours
        fridaySlots.Should().HaveCount(8); // 4 hours
    }

    [Fact]
    public async Task ToggleProfessionalAvailabilityWorkflow_ShouldAffectBooking()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);

        // Act - Disable availability
        await _professionalService.SetProfessionalAvailabilityAsync(professional.Id, false);

        // Assert
        var disabledProfessional = await _professionalRepository.GetByIdAsync(professional.Id);
        disabledProfessional!.IsAvailable.Should().BeFalse();

        // Act & Assert - Try to book - should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orderService.CreateOrderAsync(
                _clientUser!.Id,
                professional.Id,
                nextMonday.Add(TimeSpan.FromHours(10)),
                30));

        // Act - Re-enable availability
        await _professionalService.SetProfessionalAvailabilityAsync(professional.Id, true);

        // Act & Assert - Try to book - should succeed
        var order = await _orderService.CreateOrderAsync(
            _clientUser.Id,
            professional.Id,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        order.Should().NotBeNull();
    }

    #endregion

    #region Professional with Date-Ranged Availability Workflow

    [Fact]
    public async Task ProfessionalWithDateRangedAvailabilityWorkflow_ShouldRespectDateConstraints()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(21);

        // Act - Create date-ranged availability
        var availability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17),
            ScheduleType.DateRange,
            startDate,
            endDate);

        // Assert
        availability.StartDate.Should().Be(startDate);
        availability.EndDate.Should().Be(endDate);
        availability.ScheduleType.Should().Be(ScheduleType.DateRange);

        // Act - Try to generate slots before start date
        var beforeStartDate = GetNextDayOfWeek(DayOfWeek.Monday);
        var slotsBeforeStart = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, beforeStartDate);
        slotsBeforeStart.Should().BeEmpty();

        // Act - Generate slots within range
        var withinRangeDate = startDate.AddDays(((7 - (int)startDate.DayOfWeek + 7) % 7));
        var slotsWithinRange = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, withinRangeDate);
        slotsWithinRange.Should().HaveCount(16);

        // Act - Try to generate slots after end date
        var afterEndDate = endDate.AddDays(7);
        var slotsAfterEnd = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, afterEndDate);
        slotsAfterEnd.Should().BeEmpty();
    }

    #endregion

    #region Professional Booking Management Workflow

    [Fact]
    public async Task ProfessionalBookingManagementWorkflow_ShouldTrackAllBookings()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);

        // Act - Multiple clients book
        var client2 = await _fixture.CreateTestUserAsync("client2@test.com", "Alice", "Johnson");
        var client3 = await _fixture.CreateTestUserAsync("client3@test.com", "Bob", "Smith");

        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id, professional.Id, nextMonday.Add(TimeSpan.FromHours(9)), 30);

        var order2 = await _orderService.CreateOrderAsync(
            client2.Id, professional.Id, nextMonday.Add(TimeSpan.FromHours(10)), 30);

        var order3 = await _orderService.CreateOrderAsync(
            client3.Id, professional.Id, nextMonday.Add(TimeSpan.FromHours(11)), 30);

        // Assert
        var professionalOrders = await _orderService.GetOrdersByProfessionalAsync(_professionalUser.Id);
        professionalOrders.Should().HaveCount(3);

        var clients = await _orderService.GetClientsByProfessionalAsync(_professionalUser.Id);
        clients.Should().HaveCount(3);
    }

    #endregion

    #region Professional with Modified Availability Workflow

    [Fact]
    public async Task ProfessionalWithModifiedAvailabilityWorkflow_ShouldUpdateSlots()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        var availability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(12));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        var initialSlots = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);
        initialSlots.Should().HaveCount(6); // 3 hours = 6 slots

        // Act - Extend availability hours
        var updatedAvailability = await _availabilityService.UpdateAvailabilityAsync(
            availability.Id,
            startTime: TimeSpan.FromHours(9),
            endTime: TimeSpan.FromHours(17));

        // Assert
        updatedAvailability.StartTime.Should().Be(TimeSpan.FromHours(9));
        updatedAvailability.EndTime.Should().Be(TimeSpan.FromHours(17));

        // Act - Regenerate slots
        var updatedSlots = await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);

        // Assert - Should have more slots now
        updatedSlots.Should().HaveCount(16); // 8 hours = 16 slots
    }

    #endregion

    #region Professional Deletion Workflow

    [Fact]
    public async Task ProfessionalDeletionWorkflow_ShouldRemoveProfessional()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);

        var order = await _orderService.CreateOrderAsync(
            _clientUser!.Id, professional.Id, nextMonday.Add(TimeSpan.FromHours(10)), 30);

        // Act
        var deleteResult = await _professionalService.DeleteProfessionalAsync(professional.Id);

        // Assert
        deleteResult.Should().BeTrue();

        // Verify professional no longer exists
        var retrievedProfessional = await _professionalRepository.GetByIdAsync(professional.Id);
        retrievedProfessional.Should().BeNull();
    }

    #endregion

    #region Professional with Multiple Schedule Types Workflow

    [Fact]
    public async Task ProfessionalWithMultipleScheduleTypesWorkflow_ShouldSupportMixedSchedules()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        // Act - Create different schedule types
        var recurringAvailability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(13),
            ScheduleType.Recurring);

        var oneTimeAvailability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id,
            DayOfWeek.Friday,
            TimeSpan.FromHours(14),
            TimeSpan.FromHours(18),
            ScheduleType.OneTime,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7));

        var dateRangeAvailability = await _availabilityService.CreateAvailabilityAsync(
            professional.Id,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(10),
            TimeSpan.FromHours(16),
            ScheduleType.DateRange,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30));

        // Assert
        var availabilities = await _availabilityService.GetAvailabilitiesByProfessionalAsync(professional.Id);
        availabilities.Should().HaveCount(3);

        availabilities.Should().Contain(a => a.ScheduleType == ScheduleType.Recurring);
        availabilities.Should().Contain(a => a.ScheduleType == ScheduleType.OneTime);
        availabilities.Should().Contain(a => a.ScheduleType == ScheduleType.DateRange);
    }

    #endregion

    #region Professional Partial Update Workflow

    [Fact]
    public async Task ProfessionalPartialUpdateWorkflow_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.",
            qualifications: "MD",
            specialization: "General Medicine");

        // Act - Update only specialization
        var updatedProfessional = await _professionalService.UpdateProfessionalAsync(
            professional.Id,
            specialization: "Internal Medicine");

        // Assert
        updatedProfessional.Specialization.Should().Be("Internal Medicine");
        updatedProfessional.Qualifications.Should().Be("MD"); // Unchanged
        updatedProfessional.Title.Should().Be("Dr."); // Unchanged
    }

    #endregion

    #region Professional Client Interaction Workflow

    [Fact]
    public async Task ProfessionalClientInteractionWorkflow_ShouldTrackClientRelationships()
    {
        // Arrange
        var professional = await _professionalService.CreateProfessionalAsync(
            _professionalUser!.Id,
            title: "Dr.");

        await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextMonday);

        // Act - Same client books multiple times
        var order1 = await _orderService.CreateOrderAsync(
            _clientUser!.Id, professional.Id, nextMonday.Add(TimeSpan.FromHours(9)), 30);

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        await _availabilityService.CreateAvailabilityAsync(
            professional.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _availabilityService.GenerateSlotsForDateAsync(professional.Id, nextWednesday);

        var order2 = await _orderService.CreateOrderAsync(
            _clientUser.Id, professional.Id, nextWednesday.Add(TimeSpan.FromHours(14)), 30);

        // Assert
        var clients = await _orderService.GetClientsByProfessionalAsync(_professionalUser.Id);
        clients.Should().HaveCount(1); // Same client
        clients.First().Id.Should().Be(_clientUser.Id);

        var clientOrders = await _orderService.GetOrdersByClientAsync(_clientUser.Id);
        clientOrders.Should().HaveCount(2);
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