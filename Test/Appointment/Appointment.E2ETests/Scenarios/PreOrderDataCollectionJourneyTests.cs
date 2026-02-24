using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Appointment.E2ETests.Fixtures;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.E2ETests.Scenarios;

/// <summary>
/// End-to-End tests for pre-order data collection journeys
/// Module 1.5: Pre-Order Data Collection Module
/// </summary>
[Collection("E2E Tests")]
public class PreOrderDataCollectionJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public PreOrderDataCollectionJourneyTests()
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
    public async Task SubmitPreOrderData_ValidationComplete_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        // Create booking
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            60);

        // Submit pre-order data
        var dataFields = new Dictionary<string, string>
        {
            { "symptoms", "Headache" },
            { "duration", "2 weeks" },
            { "severity", "Moderate" }
        };

        var preOrderData = await _fixture.SubmitPreOrderDataAsync(
            patient.Token,
            order.Id,
            dataFields);

        preOrderData.Should().NotBeNull();
        preOrderData!.DataFields.Should().HaveCount(3);
        preOrderData.IsCompleted.Should().BeFalse();

        // Mark as complete
        var completedData = await _fixture.CompletePreOrderDataAsync(
            patient.Token,
            preOrderData.Id);

        completedData.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePreOrderData_ProgressiveSubmission_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);

        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextTuesday.Add(TimeSpan.FromHours(14)),
            45);

        // Initial submission
        var initialFields = new Dictionary<string, string>
        {
            { "symptoms", "Chest pain" }
        };

        var preOrderData = await _fixture.SubmitPreOrderDataAsync(
            patient.Token,
            order.Id,
            initialFields);

        // Update with more fields
        var updateFields = new Dictionary<string, string>
        {
            { "duration", "1 week" },
            { "severity", "Mild" }
        };

        var client = _fixture.CreateAuthenticatedClient(patient.Token);
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/preorder-data/{preOrderData!.Id}",
            new { dataFields = updateFields });

        updateResponse.EnsureSuccessStatusCode();

        var updatedData = await updateResponse.Content.ReadFromJsonAsync<PreOrderData>();
        updatedData!.DataFields.Should().HaveCount(3);
    }

    [Fact]
    public async Task ValidatePreOrderData_RequiredFieldsCheck_Success()
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
            nextWednesday.Add(TimeSpan.FromHours(11)),
            30);

        // Submit incomplete data
        var incompleteFields = new Dictionary<string, string>
        {
            { "symptoms", "" } // Empty
        };

        var preOrderData = await _fixture.SubmitPreOrderDataAsync(
            patient.Token,
            order.Id,
            incompleteFields);

        // Validate with required fields
        var requiredFields = new Dictionary<string, string>
        {
            { "symptoms", "" },
            { "duration", "" }
        };

        var client = _fixture.CreateAuthenticatedClient(patient.Token);
        var validateResponse = await client.PostAsJsonAsync(
            $"/api/preorder-data/{preOrderData!.Id}/validate",
            new { requiredFields });

        validateResponse.EnsureSuccessStatusCode();

        var result = await validateResponse.Content.ReadAsStringAsync();
        // Validation should fail due to missing/empty fields
    }

    private DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow;
        var daysUntilDay = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysUntilDay);
        return nextDay.Date;
    }
}