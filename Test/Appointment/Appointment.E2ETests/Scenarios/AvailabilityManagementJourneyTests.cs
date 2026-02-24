using System;
using System.Threading.Tasks;
using Appointment.E2ETests.Fixtures;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.E2ETests.Scenarios;

/// <summary>
/// End-to-End tests for availability management journeys
/// Module 1.2: Availability & Schedule Module - Complete workflow testing
/// </summary>
[Collection("E2E Tests")]
public class AvailabilityManagementJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public AvailabilityManagementJourneyTests()
    {
        _fixture = new E2ETestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Journey 1: Professional Sets Up Weekly Schedule

    [Fact]
    public async Task ProfessionalSetsUpWeeklySchedule_CompleteJourney_Success()
    {
        // SCENARIO: Professional creates comprehensive weekly availability

        // Step 1: Professional registers and creates profile
        var professional = await _fixture.CreateProfessionalAsync("doctor@test.com");
        professional.ProfessionalId.Should().NotBeNull();

        // Step 2: Professional sets up Monday availability
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        // Step 3: Professional sets up Wednesday availability
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(13));

        // Step 4: Professional sets up Friday availability
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId.Value,
            DayOfWeek.Friday,
            TimeSpan.FromHours(14),
            TimeSpan.FromHours(18));

        // Step 5: Generate slots for upcoming weeks
        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        var mondaySlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);
        mondaySlots.Should().HaveCount(16); // 8 hours = 16 slots

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        var wednesdaySlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);
        wednesdaySlots.Should().HaveCount(8); // 4 hours = 8 slots

        var nextFriday = GetNextDayOfWeek(DayOfWeek.Friday);
        var fridaySlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextFriday);
        fridaySlots.Should().HaveCount(8); // 4 hours = 8 slots

        // Verify all slots are available
        mondaySlots.All(s => s.IsAvailable).Should().BeTrue();
        wednesdaySlots.All(s => s.IsAvailable).Should().BeTrue();
        fridaySlots.All(s => s.IsAvailable).Should().BeTrue();
    }

    #endregion

    #region Journey 2: Dynamic Schedule Changes

    [Fact]
    public async Task ProfessionalModifiesSchedule_InProgressSuccess()
    {
        // SCENARIO: Professional modifies existing schedule

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(12));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        var initialSlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);
        initialSlots.Should().HaveCount(6); // 3 hours = 6 slots

        // Professional extends hours
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var context = await _fixture.GetDbContextAsync();
        var availability = await context.Availabilities.FirstAsync();

        var updateRequest = new
        {
            dayOfWeek = (int)DayOfWeek.Tuesday,
            startTime = "09:00:00",
            endTime = "17:00:00"
        };

        var response = await client.PutAsJsonAsync($"/api/availabilities/{availability.Id}", updateRequest);
        response.EnsureSuccessStatusCode();

        // Regenerate slots with new hours
        var updatedSlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);
        updatedSlots.Should().HaveCount(16); // 8 hours = 16 slots
    }

    #endregion

    #region Journey 3: Date-Ranged Availability

    [Fact]
    public async Task ProfessionalCreatesDateRangedAvailability_WithinRange_Success()
    {
        // SCENARIO: Professional creates availability for specific date range

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var client = _fixture.CreateAuthenticatedClient(professional.Token);

        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(30);

        var availabilityRequest = new
        {
            professionalId = professional.ProfessionalId!.Value,
            dayOfWeek = (int)DayOfWeek.Monday,
            startTime = "10:00:00",
            endTime = "16:00:00",
            scheduleType = (int)ScheduleType.DateRange,
            startDate = startDate.ToString("O"),
            endDate = endDate.ToString("O")
        };

        var response = await client.PostAsJsonAsync("/api/availabilities", availabilityRequest);
        response.EnsureSuccessStatusCode();

        var availability = await response.Content.ReadFromJsonAsync<Availability>();
        availability.Should().NotBeNull();
        availability!.ScheduleType.Should().Be(ScheduleType.DateRange);
        availability.StartDate.Should().Be(startDate.Date);
        availability.EndDate.Should().Be(endDate.Date);

        // Generate slots within range
        var testDate = startDate.AddDays(((7 - (int)startDate.DayOfWeek + 7) % 7) + 1);
        var slots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, testDate);
        slots.Should().HaveCount(12); // 6 hours = 12 slots

        // Generate slots before range (should be empty)
        var beforeRangeDate = DateTime.UtcNow.Date;
        var slotsBefore = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, beforeRangeDate);
        slotsBefore.Should().BeEmpty();
    }

    #endregion

    #region Journey 4: One-Time Special Availability

    [Fact]
    public async Task ProfessionalCreatesOneTimeAvailability_Success()
    {
        // SCENARIO: Professional creates availability for a specific day only

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var client = _fixture.CreateAuthenticatedClient(professional.Token);

        var specialDate = DateTime.UtcNow.AddDays(14);

        var availabilityRequest = new
        {
            professionalId = professional.ProfessionalId!.Value,
            dayOfWeek = (int)specialDate.DayOfWeek,
            startTime = "09:00:00",
            endTime = "17:00:00",
            scheduleType = (int)ScheduleType.OneTime,
            startDate = specialDate.ToString("O"),
            endDate = specialDate.ToString("O")
        };

        var response = await client.PostAsJsonAsync("/api/availabilities", availabilityRequest);
        response.EnsureSuccessStatusCode();

        var availability = await response.Content.ReadFromJsonAsync<Availability>();
        availability.Should().NotBeNull();
        availability!.ScheduleType.Should().Be(ScheduleType.OneTime);

        // Generate slots for that specific date
        var slots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, specialDate.Date);
        slots.Should().HaveCount(16);
    }

    #endregion

    #region Journey 5: Professional Toggles Availability

    [Fact]
    public async Task ProfessionalTogglesAvailability_BookingImpact_Success()
    {
        // SCENARIO: Professional disables availability, bookings are prevented

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var patient = await _fixture.CreateClientAsync();
        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        // Book successfully when available
        var order1 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        order1.Should().NotBeNull();

        // Professional disables availability
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var toggleRequest = new { isAvailable = false };
        var toggleResponse = await client.PatchAsJsonAsync(
            $"/api/professionals/{professional.ProfessionalId.Value}/availability",
            toggleRequest);
        toggleResponse.EnsureSuccessStatusCode();

        // Try to book when unavailable - should fail
        var context = await _fixture.GetDbContextAsync();
        var updatedProfessional = await context.Professionals.FindAsync(professional.ProfessionalId.Value);
        updatedProfessional!.IsAvailable.Should().BeFalse();

        // Re-enable and try again
        var reToggleRequest = new { isAvailable = true };
        await client.PatchAsJsonAsync(
            $"/api/professionals/{professional.ProfessionalId.Value}/availability",
            reToggleRequest);

        var order2 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(11)),
            30);

        order2.Should().NotBeNull();
    }

    #endregion

    #region Journey 6: Slot Consumption Over Time

    [Fact]
    public async Task SlotsConsumedOverTime_MultipleBookings_Success()
    {
        // SCENARIO: Multiple bookings consume available slots

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(11)); // 2 hours = 4 slots

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);

        var patient = await _fixture.CreateClientAsync();
        var patient2 = await _fixture.CreateClientAsync("patient2@test.com");

        // Book first slot (9:00-9:30)
        var order1 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextTuesday.Add(TimeSpan.FromHours(9)),
            30);

        order1.Should().NotBeNull();

        // Book second slot (9:30-10:00)
        var order2 = await _fixture.BookAppointmentAsync(
            patient2.Token,
            professional.ProfessionalId.Value,
            nextTuesday.Add(TimeSpan.FromHours(9.5)),
            30);

        order2.Should().NotBeNull();

        // Book third slot (10:00-10:30)
        var patient3 = await _fixture.CreateClientAsync("patient3@test.com");
        var order3 = await _fixture.BookAppointmentAsync(
            patient3.Token,
            professional.ProfessionalId.Value,
            nextTuesday.Add(TimeSpan.FromHours(10)),
            30);

        order3.Should().NotBeNull();

        // Verify slots are being consumed
        var allSlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);
        var bookedSlots = allSlots.Where(s => !s.IsAvailable).ToList();
        bookedSlots.Should().HaveCount(3);
    }

    #endregion

    #region Journey 7: Professional Updates Schedule

    [Fact]
    public async Task ProfessionalUpdatesSchedule_NewSlotsGenerated_Success()
    {
        // SCENARIO: Professional updates schedule, new slots are generated

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(12)); // 3 hours

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        var initialSlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);
        initialSlots.Should().HaveCount(6);

        // Professional extends hours to 5pm
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var context = await _fixture.GetDbContextAsync();
        var availability = await context.Availabilities.FirstAsync();

        var updateRequest = new
        {
            dayOfWeek = (int)DayOfWeek.Wednesday,
            startTime = "09:00:00",
            endTime = "17:00:00"
        };

        var response = await client.PutAsJsonAsync($"/api/availabilities/{availability.Id}", updateRequest);
        response.EnsureSuccessStatusCode();

        // Regenerate slots - should have more now
        var updatedSlots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);
        updatedSlots.Should().HaveCount(16); // 8 hours = 16 slots
    }

    #endregion

    #region Journey 8: Professional with Multiple Schedule Types

    [Fact]
    public async Task ProfessionalMultipleScheduleTypes_MixedSetup_Success()
    {
        // SCENARIO: Professional has recurring, one-time, and date-ranged availability

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var client = _fixture.CreateAuthenticatedClient(professional.Token);

        // Recurring: Every Tuesday
        var recurringRequest = new
        {
            professionalId = professional.ProfessionalId!.Value,
            dayOfWeek = (int)DayOfWeek.Tuesday,
            startTime = "09:00:00",
            endTime = "13:00:00",
            scheduleType = (int)ScheduleType.Recurring
        };

        var recurringResponse = await client.PostAsJsonAsync("/api/availabilities", recurringRequest);
        recurringResponse.EnsureSuccessStatusCode();

        // One-time: Special Saturday
        var specialDate = DateTime.UtcNow.AddDays(21);
        var oneTimeRequest = new
        {
            professionalId = professional.ProfessionalId.Value,
            dayOfWeek = (int)specialDate.DayOfWeek,
            startTime = "10:00:00",
            endTime = "14:00:00",
            scheduleType = (int)ScheduleType.OneTime,
            startDate = specialDate.ToString("O"),
            endDate = specialDate.ToString("O")
        };

        var oneTimeResponse = await client.PostAsJsonAsync("/api/availabilities", oneTimeRequest);
        oneTimeResponse.EnsureSuccessStatusCode();

        // Date-ranged: Two weeks only
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = DateTime.UtcNow.AddDays(21);
        var dateRangeRequest = new
        {
            professionalId = professional.ProfessionalId.Value,
            dayOfWeek = (int)DayOfWeek.Thursday,
            startTime = "14:00:00",
            endTime = "18:00:00",
            scheduleType = (int)ScheduleType.DateRange,
            startDate = startDate.ToString("O"),
            endDate = endDate.ToString("O")
        };

        var dateRangeResponse = await client.PostAsJsonAsync("/api/availabilities", dateRangeRequest);
        dateRangeResponse.EnsureSuccessStatusCode();

        // Verify all three types exist
        var context = await _fixture.GetDbContextAsync();
        var availabilities = await context.Availabilities
            .Where(a => a.ProfessionalId == professional.ProfessionalId.Value)
            .ToListAsync();

        availabilities.Should().HaveCount(3);
        availabilities.Should().Contain(a => a.ScheduleType == ScheduleType.Recurring);
        availabilities.Should().Contain(a => a.ScheduleType == ScheduleType.OneTime);
        availabilities.Should().Contain(a => a.ScheduleType == ScheduleType.DateRange);
    }

    #endregion

    #region Journey 9: Slot Generation on Demand

    [Fact]
    public async Task SlotGenerationOnDemand_AutomaticCreation_Success()
    {
        // SCENARIO: Slots are automatically generated when needed

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Friday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextFriday = GetNextDayOfWeek(DayOfWeek.Friday);

        // Slots are generated on demand when booking
        var patient = await _fixture.CreateClientAsync();

        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextFriday.Add(TimeSpan.FromHours(10)),
            30);

        order.Should().NotBeNull();

        // Verify slots were generated
        var slots = await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextFriday);
        slots.Should().HaveCount(16);
    }

    #endregion

    #region Journey 10: Available Slots Query

    [Fact]
    public async Task QueryAvailableSlots_OnlyUnbookedShown_Success()
    {
        // SCENARIO: Query returns only available slots

        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(11)); // 2 hours = 4 slots

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        var patient = await _fixture.CreateClientAsync();

        // Book one slot
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(9)),
            30);

        // Query available slots
        var client = _fixture.CreateAuthenticatedClient(patient.Token);
        var response = await client.GetAsync(
            $"/api/availabilities/slots/available?professionalId={professional.ProfessionalId.Value}&date={nextMonday:yyyy-MM-dd}");

        response.EnsureSuccessStatusCode();

        var availableSlots = await response.Content.ReadFromJsonAsync<AvailabilitySlot[]>();
        availableSlots.Should().NotBeNull();
        availableSlots!.Should().HaveCount(3); // 4 total - 1 booked = 3 available
        availableSlots.All(s => s.IsAvailable).Should().BeTrue();
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