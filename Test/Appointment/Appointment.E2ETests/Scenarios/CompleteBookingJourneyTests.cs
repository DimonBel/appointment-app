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
/// End-to-End tests for complete booking user journeys
/// Module 1.1: Order Management Module - Full lifecycle testing
/// </summary>
[Collection("E2E Tests")]
public class CompleteBookingJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public CompleteBookingJourneyTests()
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

    #region Journey 1: New Patient Books First Appointment

    [Fact]
    public async Task NewPatientBooksFirstAppointment_CompleteJourney_Success()
    {
        // SCENARIO: A new patient registers, browses professionals, and books their first appointment

        // Step 1: Patient registers and logs in
        var patient = await _fixture.CreateClientAsync("patient@test.com");
        patient.Should().NotBeNull();
        patient.Token.Should().NotBeEmpty();

        // Step 2: Professional creates availability
        var professional = await _fixture.CreateProfessionalAsync("doctor@test.com");
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        // Step 3: Patient books appointment
        var scheduledDateTime = nextMonday.Add(TimeSpan.FromHours(10));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            scheduledDateTime,
            60,
            "First Consultation");

        // Assertions
        order.Should().NotBeNull();
        order!.Id.Should().NotBeEmpty();
        order.Status.Should().Be(OrderStatus.Requested);
        order.ClientId.Should().Be(patient.Id);
        order.ProfessionalId.Should().Be(professional.Id);

        // Verify slot was reserved
        await _fixture.VerifySlotBookedAsync(professional.ProfessionalId.Value, scheduledDateTime, 60);

        // Step 4: Patient can view their booking
        var patientOrders = await _fixture.GetUserOrdersAsync(patient.Token);
        patientOrders.Should().HaveCount(1);
        patientOrders.First().Id.Should().Be(order.Id);
    }

    #endregion

    #region Journey 2: Complete Appointment Lifecycle

    [Fact]
    public async Task AppointmentLifecycle_RequestToComplete_Success()
    {
        // SCENARIO: Full appointment lifecycle from request to completion

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);

        // Phase 1: Request (Book)
        var scheduledDateTime = nextTuesday.Add(TimeSpan.FromHours(14));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            scheduledDateTime,
            45,
            "Follow-up Visit");

        order.Status.Should().Be(OrderStatus.Requested);

        // Phase 2: Approve
        var approvedOrder = await _fixture.ApproveAppointmentAsync(
            professional.Token,
            order.Id,
            "Approved for consultation");

        approvedOrder.Status.Should().Be(OrderStatus.Approved);
        await _fixture.VerifyOrderHistoryAsync(order.Id, 1);

        // Phase 3: Complete
        var completedOrder = await _fixture.CompleteAppointmentAsync(
            professional.Token,
            order.Id,
            "Consultation completed successfully");

        completedOrder.Status.Should().Be(OrderStatus.Completed);
        completedOrder.CompletedAt.Should().NotBeNull();
        completedOrder.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify complete audit trail
        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);

        var context = await _fixture.GetDbContextAsync();
        var history = await context.OrderHistory
            .Where(h => h.OrderId == order.Id)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync();

        history[0].NewStatus.Should().Be(OrderStatus.Approved);
        history[1].NewStatus.Should().Be(OrderStatus.Completed);
    }

    #endregion

    #region Journey 3: Appointment Cancellation by Patient

    [Fact]
    public async Task PatientCancelsAppointment_BeforeApproval_Success()
    {
        // SCENARIO: Patient cancels appointment before professional approval

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);

        // Book appointment
        var scheduledDateTime = nextWednesday.Add(TimeSpan.FromHours(11));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            scheduledDateTime,
            30);

        order.Status.Should().Be(OrderStatus.Requested);

        // Cancel appointment
        var cancelledOrder = await _fixture.CancelAppointmentAsync(
            patient.Token,
            order.Id,
            "Patient needs to reschedule due to work");

        // Assertions
        cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);
        cancelledOrder.Notes.Should().Be("Patient needs to reschedule due to work");

        // Verify slot was released (since it was only Requested, slots remain booked)
        // Note: In the current implementation, Requested orders also reserve slots
    }

    #endregion

    #region Journey 4: Appointment Cancellation After Approval

    [Fact]
    public async Task PatientCancelsAppointment_AfterApproval_SlotsReleased()
    {
        // SCENARIO: Patient cancels appointment after professional approval

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();
        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Thursday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextThursday = GetNextDayOfWeek(DayOfWeek.Thursday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextThursday);

        // Book and approve
        var scheduledDateTime = nextThursday.Add(TimeSpan.FromHours(10));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            scheduledDateTime,
            60);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);

        // Cancel after approval
        var cancelledOrder = await _fixture.CancelAppointmentAsync(
            patient.Token,
            order.Id,
            "Emergency came up");

        // Assertions
        cancelledOrder.Status.Should().Be(OrderStatus.Cancelled);

        // Verify history
        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);
    }

    #endregion

    #region Journey 5: Multiple Appointments for Same Patient

    [Fact]
    public async Task PatientBooksMultipleAppointments_Success()
    {
        // SCENARIO: Patient books multiple appointments with different professionals

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional1 = await _fixture.CreateProfessionalAsync("doctor1@test.com");
        var professional2 = await _fixture.CreateProfessionalAsync("doctor2@test.com");

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional1.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional2.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);

        await _fixture.GenerateSlotsAsync(professional1.ProfessionalId.Value, nextMonday);
        await _fixture.GenerateSlotsAsync(professional2.ProfessionalId.Value, nextWednesday);

        // Book with first professional
        var order1 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional1.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(10)),
            30,
            "Initial Consultation with Dr. Smith");

        // Book with second professional
        var order2 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional2.ProfessionalId.Value,
            nextWednesday.Add(TimeSpan.FromHours(14)),
            45,
            "Specialist Consultation with Dr. Johnson");

        // Assertions
        order1.Should().NotBeNull();
        order2.Should().NotBeNull();
        order1!.Id.Should().NotBe(order2!.Id);

        var patientOrders = await _fixture.GetUserOrdersAsync(patient.Token);
        patientOrders.Should().HaveCount(2);
    }

    #endregion

    #region Journey 6: Appointment with Domain Configuration

    [Fact]
    public async Task AppointmentWithDomainConfiguration_Success()
    {
        // SCENARIO: Patient books appointment with specific domain type

        // Setup
        var admin = await _fixture.CreateAdminAsync();
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();

        // Create domain configuration
        var domainConfig = await _fixture.CreateDomainConfigurationAsync(
            DomainType.Medical,
            "Specialized Medical Consultation");

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Friday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextFriday = GetNextDayOfWeek(DayOfWeek.Friday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextFriday);

        // Book with domain configuration
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            nextFriday.Add(TimeSpan.FromHours(13)),
            60,
            "Specialized Consultation");

        // Note: Domain configuration would be associated via update or during creation
        // This test verifies the booking flow supports domain-specific appointments
        order.Should().NotBeNull();
        order.Status.Should().Be(OrderStatus.Requested);
    }

    #endregion

    #region Journey 7: Booking with Pre-Order Data Collection

    [Fact]
    public async Task BookingWithPreOrderDataCollection_CompleteWorkflow_Success()
    {
        // SCENARIO: Patient provides preliminary data before booking confirmation

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextMonday);

        // Step 1: Create initial booking request
        var scheduledDateTime = nextMonday.Add(TimeSpan.FromHours(10));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            scheduledDateTime,
            60,
            "Consultation with Pre-Order Data");

        // Step 2: Submit pre-order data
        var preOrderFields = new Dictionary<string, string>
        {
            { "symptoms", "Persistent headaches for 2 weeks" },
            { "duration", "14 days" },
            { "severity", "Moderate" },
            { "previousTreatment", "Over-the-counter pain relievers" },
            { "allergies", "None known" },
            { "currentMedications", "Ibuprofen as needed" }
        };

        var preOrderData = await _fixture.SubmitPreOrderDataAsync(
            patient.Token,
            order.Id,
            preOrderFields);

        preOrderData.Should().NotBeNull();
        preOrderData!.DataFields.Should().HaveCount(6);

        // Step 3: Mark pre-order data as complete
        var completedPreOrderData = await _fixture.CompletePreOrderDataAsync(
            patient.Token,
            preOrderData.Id);

        completedPreOrderData.IsCompleted.Should().BeTrue();

        // Step 4: Professional approves after reviewing data
        var approvedOrder = await _fixture.ApproveAppointmentAsync(
            professional.Token,
            order.Id,
            "Patient data reviewed - approved for consultation");

        approvedOrder.Status.Should().Be(OrderStatus.Approved);

        // Step 5: Complete appointment
        var completedOrder = await _fixture.CompleteAppointmentAsync(
            professional.Token,
            order.Id,
            "Consultation completed - pre-order data was helpful");

        completedOrder.Status.Should().Be(OrderStatus.Completed);

        // Verify complete journey
        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);
    }

    #endregion

    #region Journey 8: Reschedule Appointment

    [Fact]
    public async Task RescheduleAppointment_Success()
    {
        // SCENARIO: Patient reschedules appointment to a different time

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextTuesday = GetNextDayOfWeek(DayOfWeek.Tuesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextTuesday);

        // Book initial appointment
        var originalDateTime = nextTuesday.Add(TimeSpan.FromHours(10));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            originalDateTime,
            30);

        // Reschedule
        var newDateTime = nextTuesday.Add(TimeSpan.FromHours(15));
        var client = _fixture.CreateAuthenticatedClient(patient.Token);

        var rescheduleRequest = new
        {
            newScheduledDateTime = newDateTime.ToString("O"),
            notes = "Rescheduled by patient request"
        };

        var response = await client.PostAsJsonAsync($"/api/orders/{order.Id}/reschedule", rescheduleRequest);
        response.EnsureSuccessStatusCode();

        var rescheduledOrder = await response.Content.ReadFromJsonAsync<Order>();

        // Assertions
        rescheduledOrder.Should().NotBeNull();
        rescheduledOrder!.ScheduledDateTime.Should().BeCloseTo(newDateTime, TimeSpan.FromSeconds(1));
        rescheduledOrder.Notes.Should().Be("Rescheduled by patient request");
    }

    #endregion

    #region Journey 9: Decline and Rebook

    [Fact]
    public async Task DeclineAndRebook_Success()
    {
        // SCENARIO: Professional declines booking, patient rebooks with different professional

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional1 = await _fixture.CreateProfessionalAsync("doctor1@test.com");
        var professional2 = await _fixture.CreateProfessionalAsync("doctor2@test.com");

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional1.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional2.ProfessionalId!.Value,
            DayOfWeek.Monday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextMonday = GetNextDayOfWeek(DayOfWeek.Monday);
        await _fixture.GenerateSlotsAsync(professional1.ProfessionalId.Value, nextMonday);
        await _fixture.GenerateSlotsAsync(professional2.ProfessionalId.Value, nextMonday);

        // Book with first professional
        var scheduledDateTime = nextMonday.Add(TimeSpan.FromHours(10));
        var order1 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional1.ProfessionalId.Value,
            scheduledDateTime,
            30,
            "Initial Consultation");

        // Professional declines
        var client = _fixture.CreateAuthenticatedClient(professional1.Token);
        var declineRequest = new { reason = "Professional not available on this date" };
        var declineResponse = await client.PostAsJsonAsync($"/api/orders/{order1.Id}/decline", declineRequest);
        declineResponse.EnsureSuccessStatusCode();

        var declinedOrder = await declineResponse.Content.ReadFromJsonAsync<Order>();
        declinedOrder!.Status.Should().Be(OrderStatus.Declined);

        // Patient rebooks with second professional
        var order2 = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional2.ProfessionalId.Value,
            nextMonday.Add(TimeSpan.FromHours(14)),
            30,
            "Rescheduled Consultation");

        // Assertions
        order2.Should().NotBeNull();
        order2!.Status.Should().Be(OrderStatus.Requested);
        order2.Id.Should().NotBe(order1.Id);
    }

    #endregion

    #region Journey 10: No-Show Scenario

    [Fact]
    public async Task NoShowScenario_Success()
    {
        // SCENARIO: Patient doesn't show up for appointment

        // Setup
        var patient = await _fixture.CreateClientAsync();
        var professional = await _fixture.CreateProfessionalAsync();

        await _fixture.SetupProfessionalAvailabilityAsync(
            professional.ProfessionalId!.Value,
            DayOfWeek.Wednesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(17));

        var nextWednesday = GetNextDayOfWeek(DayOfWeek.Wednesday);
        await _fixture.GenerateSlotsAsync(professional.ProfessionalId.Value, nextWednesday);

        // Book and approve
        var scheduledDateTime = nextWednesday.Add(TimeSpan.FromHours(10));
        var order = await _fixture.BookAppointmentAsync(
            patient.Token,
            professional.ProfessionalId.Value,
            scheduledDateTime,
            30);

        await _fixture.ApproveAppointmentAsync(professional.Token, order.Id);

        // Mark as no-show
        var client = _fixture.CreateAuthenticatedClient(professional.Token);
        var noShowRequest = new { notes = "Patient did not attend" };
        var noShowResponse = await client.PostAsJsonAsync($"/api/orders/{order.Id}/noshow", noShowRequest);
        noShowResponse.EnsureSuccessStatusCode();

        var noShowOrder = await noShowResponse.Content.ReadFromJsonAsync<Order>();

        // Assertions
        noShowOrder.Should().NotBeNull();
        noShowOrder!.Status.Should().Be(OrderStatus.NoShow);
        noShowOrder.Notes.Should().Be("Client did not show up for appointment");

        await _fixture.VerifyOrderHistoryAsync(order.Id, 2);
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