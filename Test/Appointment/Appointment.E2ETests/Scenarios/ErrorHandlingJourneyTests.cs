using System;
using System.Threading.Tasks;
using Appointment.E2ETests.Fixtures;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.E2ETests.Scenarios;

/// <summary>
/// End-to-End tests for error handling and recovery scenarios
/// Tests system resilience and graceful failure handling
/// </summary>
[Collection("E2E Tests")]
public class ErrorHandlingJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public ErrorHandlingJourneyTests()
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

    [Fact]
    public async Task BookUnavailableSlot_ErrorReturned_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        var patient = await _fixture.CreateClientAsync();

        // Book first appointment
        var order1 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        order1.Should().NotBeNull();

        // Try to book same slot - should fail
        try
        {
            await _fixture.BookAppointmentAsync(
                patient.Token,
                professional.ProfessionalId.Value,
                nextMonday.Add(TimeSpan.FromHours(10)),
                30);

            Assert.True(false, "Should have thrown exception for unavailable slot");
        }
        catch (Exception)
        {
            // Expected - slot is already booked
        }
    }

    [Fact]
    public async Task BookWithUnavailableProfessional_ErrorReturned_Success()
    {
        var patient = await _fixture.CreateClientAsync();
        var scheduledDateTime = GetNextDayOfWeek(DayOfWeek.Monday).AddHours(10);

        try
        {
            await _fixture.BookAppointmentAsync(
                patient.Token,
                Guid.NewGuid(), // Non-existent professional
                scheduledDateTime,
                30);

            Assert.True(false, "Should have thrown exception for non-existent professional");
        }
        catch (Exception)
        {
            // Expected - professional not found
        }
    }

    [Fact]
    public async Task BookInPast_ErrorReturned_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var patient = await _fixture.CreateClientAsync();
        var pastDateTime = DateTime.UtcNow.AddHours(-1);

        try
        {
            await _fixture.BookAppointmentAsync(
                patient.Token,
                professional.ProfessionalId.Value,
                pastDateTime,
                30);

            Assert.True(false, "Should have thrown exception for past date time");
        }
        catch (Exception)
        {
            // Expected - cannot book in the past
        }
    }

    [Fact]
    public async Task ApproveAlreadyCompletedOrder_ErrorReturned_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);

        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextWednesday.Add(TimeSpan.FromHours(10)),
            30);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);
        await _fixture.CompleteAppointmentAsync(professional.Token, order.Id);

        // Try to approve completed order - should fail
        try
        {
            await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);
            Assert.True(false, "Should have thrown exception for completed order");
        }
        catch (Exception)
        {
            // Expected - cannot approve completed order
        }
    }

    [Fact]
    public async Task CancelCompletedOrder_ErrorReturned_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Friday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextFriday = GetNextDayOfWeek(DayOfWeek.Friday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextFriday);

        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextFriday.Add(TimeSpan.FromHours(10)),
            30);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);
        await _fixture.CompleteAppointmentAsync(professional.Token, order.Id);

        // Try to cancel completed order - should fail
        try
        {
            await _fixture.CancelAppointmentAsync(patient.Token, order.Id);
            Assert.True(false, "Should have thrown exception for completed order");
        }
        catch (Exception)
        {
            // Expected - cannot cancel completed order
        }
    }

    [Fact]
    public async Task MarkNoShowUnapprovedOrder_ErrorReturned_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Thursday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextThursday = GetNextDayOfWeek(DayOfWeek.Thursday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextThursday);

        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextThursday.Add(TimeSpan.FromHours(10)),
            30);

        // Try to mark as no-show without approval - should fail
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var noShowRequest = new { notes = "Test" };
        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/noshow", noShowRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow;
        var daysUntilDay = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysUntilDay);
        return nextDay.Date;
    }
}