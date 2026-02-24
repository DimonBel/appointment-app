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
/// Integration tests for Availability & Schedule Module (1.2)
/// Tests end-to-end workflows with real database operations including slot generation
/// </summary>
[Collection("TestDatabase")]
public class AvailabilityScheduleIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IAvailabilityService _availabilityService;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly IAvailabilityRepository _availabilityRepository;

    private AppIdentityUser? _professionalUser;
    private Professional? _professional;

    public AvailabilityScheduleIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var scope = _fixture.ServiceProvider.CreateScope();

        _availabilityService = scope.ServiceProvider.GetRequiredService<IAvailabilityService>();
        _professionalRepository = scope.ServiceProvider.GetRequiredService<IProfessionalRepository>();
        _availabilitySlotRepository = scope.ServiceProvider.GetRequiredService<IAvailabilitySlotRepository>();
        _availabilityRepository = scope.ServiceProvider.GetRequiredService<IAvailabilityRepository>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _professionalUser = await _fixture.CreateTestUserAsync("doctor@test.com", "Dr. Jane", "Smith");
        _professional = await _fixture.CreateTestProfessionalAsync(_professionalUser, "Dr.", "General Medicine");
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Complete Availability Creation Workflow

    [Fact]
    public async Task CompleteAvailabilityCreationWorkflow_ShouldCreateAvailabilityAndSlots()
    {
        // Arrange
        var dayOfWeek = DayOfWeek.Monday;
        var startTime = TimeSpan.FromHours(9);
        var endTime = TimeSpan.FromHours(17);

        // Act
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, dayOfWeek, startTime, endTime, ScheduleType.Recurring);

        // Assert
        availability.Should().NotBeNull();
        availability.Id.Should().NotBeEmpty();
        availability.ProfessionalId.Should().Be(_professional.Id);
        availability.DayOfWeek.Should().Be(dayOfWeek);
        availability.StartTime.Should().Be(startTime);
        availability.EndTime.Should().Be(endTime);
        availability.ScheduleType.Should().Be(ScheduleType.Recurring);
        availability.IsActive.Should().BeTrue();
        availability.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify persistence
        var retrievedAvailability = await _availabilityRepository.GetByIdAsync(availability.Id);
        retrievedAvailability.Should().NotBeNull();
        retrievedAvailability!.Id.Should().Be(availability.Id);
    }

    [Fact]
    public async Task AvailabilityWithDateRange_ShouldRespectDateConstraints()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(30);

        // Act
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17),
            ScheduleType.DateRange, startDate, endDate);

        // Assert
        availability.StartDate.Should().Be(startDate);
        availability.EndDate.Should().Be(endDate);
        availability.ScheduleType.Should().Be(ScheduleType.DateRange);
    }

    #endregion

    #region Slot Generation Workflow

    [Fact]
    public async Task SlotGenerationWorkflow_ShouldGenerateCorrectNumberOfSlots()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        // 9:00-10:00 should generate 2 slots: 9:00-9:30 and 9:30-10:00
        slots.Should().HaveCount(2);
        slots.All(s => s.AvailabilityId == availability.Id).Should().BeTrue();
        slots.All(s => s.SlotDate == testDate.Date).Should().BeTrue();
        slots.All(s => s.IsAvailable).Should().BeTrue();

        // Verify specific slot times
        slots.Should().Contain(s => s.StartTime == TimeSpan.FromHours(9) && s.EndTime == TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)));
        slots.Should().Contain(s => s.StartTime == TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)) && s.EndTime == TimeSpan.FromHours(10));
    }

    [Fact]
    public async Task SlotGenerationForFullDay_ShouldGenerateAllSlots()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var testDate = GetNextDayOfWeek(DayOfWeek.Tuesday);

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        // 8 hours = 480 minutes / 30 minutes per slot = 16 slots
        slots.Should().HaveCount(16);
    }

    [Fact]
    public async Task SlotGenerationWithExistingSlots_ShouldSkipExisting()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        var testDate = GetNextDayOfWeek(DayOfWeek.Wednesday);

        // Generate initial slots
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);
        var initialSlotCount = (await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, testDate)).Count();

        // Act - Generate again
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert - Should return existing slots, not create duplicates
        slots.Should().HaveCount(initialSlotCount);
    }

    [Fact]
    public async Task SlotGenerationForNonMatchingDay_ShouldGenerateNoSlots()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        var testDate = GetNextDayOfWeek(DayOfWeek.Tuesday); // Different day

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task SlotGenerationForInactiveAvailability_ShouldGenerateNoSlots()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Deactivate availability
        availability.IsActive = false;
        await _availabilityRepository.UpdateAsync(availability);

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task SlotGenerationBeforeStartDate_ShouldNotGenerateSlots()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(14); // 2 weeks from now
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        availability.StartDate = startDate;
        await _availabilityRepository.UpdateAsync(availability);

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday); // This week

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task SlotGenerationAfterEndDate_ShouldNotGenerateSlots()
    {
        // Arrange
        var endDate = DateTime.UtcNow.AddDays(-1); // Yesterday
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        availability.EndDate = endDate;
        await _availabilityRepository.UpdateAsync(availability);

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday); // This week

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        slots.Should().BeEmpty();
    }

    #endregion

    #region Get Slots Workflow

    [Fact]
    public async Task GetSlotsByDateWorkflow_ShouldReturnGeneratedSlots()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);

        // Act
        var slots = await _availabilityService.GetSlotsByDateAsync(_professional.Id, testDate);

        // Assert
        slots.Should().NotBeEmpty();
        slots.All(s => s.SlotDate == testDate.Date).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableSlotsWorkflow_ShouldReturnOnlyAvailableSlots()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);

        // Generate slots
        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Manually mark one slot as unavailable
        var allSlots = await _availabilitySlotRepository.GetSlotsByDateAsync(_professional.Id, testDate);
        var firstSlot = allSlots.First();
        firstSlot.IsAvailable = false;
        await _availabilitySlotRepository.UpdateAsync(firstSlot);

        // Act
        var availableSlots = await _availabilityService.GetAvailableSlotsAsync(_professional.Id, testDate);

        // Assert
        availableSlots.Should().HaveCount(allSlots.Count() - 1);
        availableSlots.Should().NotContain(s => s.Id == firstSlot.Id);
        availableSlots.All(s => s.IsAvailable).Should().BeTrue();
    }

    [Fact]
    public async Task IsSlotAvailableWorkflow_ShouldReturnCorrectAvailability()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);
        var testDateTime = testDate.Add(TimeSpan.FromHours(9));

        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Act
        var isAvailable = await _availabilityService.IsSlotAvailableAsync(_professional.Id, testDateTime, 30);

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlotAvailableWithInsufficientDuration_ShouldReturnFalse()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);
        var testDateTime = testDate.Add(TimeSpan.FromHours(9));

        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Act - Request 60 minutes but slot is only 30 minutes
        var isAvailable = await _availabilityService.IsSlotAvailableAsync(_professional.Id, testDateTime, 60);

        // Assert
        isAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlotAvailableForBookedSlot_ShouldReturnFalse()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);
        var testDateTime = testDate.Add(TimeSpan.FromHours(9));

        await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Mark slot as booked
        var slot = await _availabilitySlotRepository.GetSlotByDateTimeAsync(_professional.Id, testDateTime);
        slot!.IsAvailable = false;
        await _availabilitySlotRepository.UpdateAsync(slot);

        // Act
        var isAvailable = await _availabilityService.IsSlotAvailableAsync(_professional.Id, testDateTime, 30);

        // Assert
        isAvailable.Should().BeFalse();
    }

    #endregion

    #region Availability Update Workflow

    [Fact]
    public async Task UpdateAvailabilityWorkflow_ShouldPersistChanges()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Act
        var updatedAvailability = await _availabilityService.UpdateAvailabilityAsync(
            availability.Id,
            dayOfWeek: DayOfWeek.Tuesday,
            startTime: TimeSpan.FromHours(10),
            endTime: TimeSpan.FromHours(18));

        // Assert
        updatedAvailability.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        updatedAvailability.StartTime.Should().Be(TimeSpan.FromHours(10));
        updatedAvailability.EndTime.Should().Be(TimeSpan.FromHours(18));
        updatedAvailability.UpdatedAt.Should().NotBeNull();

        // Verify persistence
        var retrievedAvailability = await _availabilityRepository.GetByIdAsync(availability.Id);
        retrievedAvailability!.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        retrievedAvailability.StartTime.Should().Be(TimeSpan.FromHours(10));
    }

    [Fact]
    public async Task UpdateAvailabilityWithInvalidTimeRange_ShouldThrowException()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _availabilityService.UpdateAvailabilityAsync(
                availability.Id,
                startTime: TimeSpan.FromHours(18),
                endTime: TimeSpan.FromHours(9)));
    }

    #endregion

    #region Multiple Availabilities Workflow

    [Fact]
    public async Task MultipleAvailabilitiesForSameDay_ShouldGenerateCombinedSlots()
    {
        // Arrange
        var availability1 = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(12));
        var availability2 = await _fixture.CreateTestAvailabilityAsync(
            _professional.Id, DayOfWeek.Monday, TimeSpan.FromHours(14), TimeSpan.FromHours(17));

        var testDate = GetNextDayOfWeek(DayOfWeek.Monday);

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, testDate);

        // Assert
        // First availability: 3 hours = 6 slots
        // Second availability: 3 hours = 6 slots
        // Total: 12 slots
        slots.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetAvailabilitiesByProfessionalWorkflow_ShouldReturnAllAvailabilities()
    {
        // Arrange
        await _fixture.CreateTestAvailabilityAsync(_professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _fixture.CreateTestAvailabilityAsync(_professional.Id, DayOfWeek.Tuesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));
        await _fixture.CreateTestAvailabilityAsync(_professional.Id, DayOfWeek.Wednesday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Act
        var availabilities = await _availabilityService.GetAvailabilitiesByProfessionalAsync(_professional.Id);

        // Assert
        availabilities.Should().HaveCount(3);
        availabilities.All(a => a.ProfessionalId == _professional.Id).Should().BeTrue();
    }

    #endregion

    #region Availability Deletion Workflow

    [Fact]
    public async Task DeleteAvailabilityWorkflow_ShouldRemoveFromDatabase()
    {
        // Arrange
        var availability = await _fixture.CreateTestAvailabilityAsync(
            _professional!.Id, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(17));

        // Act
        var deleteResult = await _availabilityService.DeleteAvailabilityAsync(availability.Id);

        // Assert
        deleteResult.Should().BeTrue();

        // Verify availability no longer exists
        var retrievedAvailability = await _availabilityRepository.GetByIdAsync(availability.Id);
        retrievedAvailability.Should().BeNull();
    }

    #endregion

    #region One-Time Schedule Workflow

    [Fact]
    public async Task OneTimeAvailabilityWorkflow_ShouldGenerateSlotsForSpecificDate()
    {
        // Arrange
        var specificDate = DateTime.UtcNow.AddDays(7);
        var availability = await _availabilityService.CreateAvailabilityAsync(
            _professional!.Id,
            specificDate.DayOfWeek,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17),
            ScheduleType.OneTime,
            specificDate,
            specificDate);

        // Act
        var slots = await _availabilityService.GenerateSlotsForDateAsync(_professional.Id, specificDate);

        // Assert
        slots.Should().HaveCount(16); // 8 hours = 16 slots
        slots.All(s => s.AvailabilityId == availability.Id).Should().BeTrue();
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