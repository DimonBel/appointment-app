using System;
using System.Threading.Tasks;
using Appointment.E2ETests.Fixtures;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.E2ETests.Scenarios;

/// <summary>
/// End-to-End tests for multi-user booking scenarios
/// Tests concurrent bookings, professional schedules, and user interactions
/// </summary>
[Collection("E2E Tests")]
public class MultiUserBookingJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public MultiUserBookingJourneyTests()
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
    public async Task MultiplePatientsBookWithSameProfessional_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        var patient1 = await _fixture.CreateClientAsync("patient1@test.com");
        var patient2 = await _fixture.CreateClientAsync("patient2@test.com");
        var patient3 = await _factory.CreateClientAsync("patient3@test.com");

        var order1 = await _fixture.BookAppointmentAsync(
            patient1.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(9)),
            30);

        var order2 = await _fixture.BookAppointmentAsync(
            patient2.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        var order3 = await _fixture.BookAppointmentAsync(
            patient3.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(11)),
            30);

        order1.Should().NotBeNull();
        order2.Should().NotBeNull();
        order3.Should().NotBeNull();
        order1!.Id.Should().NotBe(order2!.Id);
        order2.Id.Should().NotBe(order3!.Id);

        var professionalOrders = await _fixture.GetUserOrdersAsync(professional.Token);
        professionalOrders.Should().HaveCount(3);
    }

    [Fact]
    public async Task PatientBooksWithMultipleProfessionals_Success()
    {
        var professional1 = await _fixture.CreateProfessionalAsync("doctor1@test.com");
        var professional2 = await _fixture.CreateProfessionalAsync("doctor2@test.com");

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional1.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional2.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);

        await _fixture.GenerateSlotsAsync(professional1.ProfessionalId.Value, nextTuesday);
        await _fixture.GenerateSlotsAsync(professional2.ProfessionalId.Value, nextWednesday);

        var patient = await _fixture.CreateClientAsync();

        var order1 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional1.ProfessionalId.Value,
            nextTuesday.Add(TimeSpan.FromHours(10)),
            30);

        var order2 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional2.ProfessionalId.Value,
            nextWednesday.Add(TimeSpan.FromHours(14)),
            45);

        order1.Should().NotBeNull();
        order2.Should().NotBeNull();
        order1!.Id.Should().NotBe(order2!.Id);

        var patientOrders = await _fixture.GetUserOrdersAsync(patient.Token);
        patientOrders.Should().HaveCount(2);
    }

    [Fact]
    public async Task ProfessionalSeesAllClients_Success()
    {
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        var patient1 = await _fixture.CreateClientAsync("patient1@test.com");
        var patient2 = await _fixture.CreateClientAsync("patient2@test.com");

        await _fixture.BookAppointmentAsync(
            patient1.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(9)),
            30);

        await _fixture.BookAppointmentAsync(
            patient2.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30);

        var professionalOrders = await _fixture.GetUserOrdersAsync(professional.Token);
        professionalOrders.Should().HaveCount(2);

        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var response = await client.GetAsync($"/api/orders/professional/{professional.Id}");
        response.EnsureSuccessStatusCode();
    }

    private DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow;
        var daysUntilDay = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysUntilDay);
        return nextDay.Date;
    }
}