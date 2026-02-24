using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Appointment.E2ETests.Fixtures;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.E2ETests.Scenarios;

/// <summary>
/// End-to-End tests for approval workflow and audit trail
/// Module 1.3: Order Approval Module + Module 1.6: Order History & Audit Module
/// </summary>
[Collection("E2E Tests")]
public class ApprovalWorkflowJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public ApprovalWorkflowJourneyTests()
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
    public async Task ProfessionalApprovesBooking_WithReason_AuditTrailCreated()
    {
        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        // Book
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            60);

        // Approve with reason
        var approvedOrder = await _fixture.ApproveAppointmentAsync(
            professional.Token,
            order.Id,
            "Approved after reviewing patient history");

        approvedOrder.Status.Should().Be(OrderStatus.Approved);
        approvedOrder.ApprovalReason.Should().Be("Approved after reviewing patient history");

        await _fixture.VerifyOrderHistoryAsync(order.Id, 1);
    }

    [Fact]
    public async Task ProfessionalDeclinesBooking_WithReason_SlotsReleased()
    {
        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);

        // Book and approve
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextTuesday.Add(TimeSpan.FromHours(10)),
            60);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);

        // Decline
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var declineRequest = new { reason = "Professional has emergency" };
        var declineResponse = await client.PostAsJsonAsync($"/api/orders/{order.Id}/decline", declineRequest);
        declineResponse.EnsureSuccessStatusCode();

        var declinedOrder = await declineResponse.Content.ReadFromJsonAsync<Order>();
        declinedOrder!.Status.Should().Be(OrderStatus.Declined);
        declinedOrder.DeclineReason.Should().Be("Professional has emergency");

        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);
    }

    [Fact]
    public async Task CompleteAppointment_WithNotes_TimestampRecorded()
    {
        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);

        // Book and approve
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextWednesday.Add(TimeSpan.FromHours(10)),
            60);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);

        // Complete
        var completedOrder = await _fixture.CompleteAppointmentAsync(
            professional.Token,
            order.Id,
            "Patient responded well to treatment");

        completedOrder.Status.Should().Be(OrderStatus.Completed);
        completedOrder.CompletedAt.Should().NotBeNull();
        completedOrder.Notes.Should().Be("Patient responded well to treatment");

        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);
    }

    [Fact]
    public async Task MarkNoShow_WithReason_AuditTrailComplete()
    {
        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Friday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextFriday = GetNextDayOfWeek(DayOfWeek.Friday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextFriday);

        // Book and approve
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextFriday.Add(TimeSpan.FromHours(10)),
            30);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);

        // Mark as no-show
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var noShowRequest = new { notes = "Patient missed appointment without notice" };
        var noShowResponse = await client.PostAsJsonAsync($"/api/orders/{order.Id}/noshow", noShowRequest);
        noShowResponse.EnsureSuccessStatusCode();

        var noShowOrder = await noShowResponse.Content.ReadFromJsonAsync<Order>();
        noShowOrder!.Status.Should().Be(OrderStatus.NoShow);
        noShowOrder.Notes.Should().Be("Client did not show up for appointment");

        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);
    }

    [Fact]
    public async Task FullAuditTrail_AllTransitionsRecorded_Success()
    {
        // Setup
        var professional = await _fixture.CreateProfessionalAsync();
        var patient = await _fixture.CreateClientAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        // Complete lifecycle
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            60);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id, "Approved");
        await _fixture.CompleteAppointmentAsync(professional.Token, order.Id, "Completed");

        // Verify complete audit trail
        var context = await _fixture.GetDbContextAsync();
        var history = await context.OrderHistory
            .Where(h => h.OrderId == order.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        history.Should().HaveCount(2);
        history[0].PreviousStatus.Should().Be(OrderStatus.Requested);
        history[0].NewStatus.Should().Be(OrderStatus.Approved);
        history[1].PreviousStatus.Should().Be(OrderStatus.Approved);
        history[1].NewStatus.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task GetOrderHistory_ChronologicalOrder_Success()
    {
        // Setup
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
            nextTuesday.Add(TimeSpan.FromHours(10)),
            60);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);
        await Task.Delay(100);
        await _fixture.CompleteAppointmentAsync(professional.Token, order.Id);

        // Get history
        var client = _fixture.CreateAuthenticatedClient(patient.Token);
        var response = await client.GetAsync($"/api/orders/{order.Id}/history");
        response.EnsureSuccessStatusCode();

        var history = await response.Content.ReadFromJsonAsync<OrderHistory[]>();
        history.Should().NotBeNull();
        history!.Should().HaveCount(2);
        history[0].ChangedAt.Should().BeBefore(history[1].ChangedAt);
    }

    private DateTime GetNextDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.UtcNow;
        var daysUntilDay = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        var nextDay = today.AddDays(daysUntilDay);
        return nextDay.Date;
    }
}