using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.API.Endpoints;

public static class AvailabilityEndpoints
{
    public static void MapAvailabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/availabilities")
            .WithTags("Availabilities");
            // .RequireAuthorization(); // Temporarily disabled for testing

        // Create availability
        group.MapPost("/", async (
            [FromBody] CreateAvailabilityDto dto,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var availability = await availabilityService.CreateAvailabilityAsync(
                dto.ProfessionalId,
                dto.DayOfWeek,
                dto.StartTime,
                dto.EndTime,
                dto.ScheduleType,
                dto.StartDate,
                dto.EndDate);

            return Results.Created($"/api/availabilities/{availability.Id}", availability);
        })
        .WithName("CreateAvailability")
        .WithOpenApi();

        // Get availability by ID
        group.MapGet("/{id}", async (
            Guid id,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var availability = await availabilityService.GetAvailabilityByIdAsync(id);
            return availability != null ? Results.Ok(availability) : Results.NotFound();
        })
        .WithName("GetAvailabilityById")
        .WithOpenApi();

        // Get availabilities by professional
        group.MapGet("/professional/{professionalId}", async (
            Guid professionalId,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var availabilities = await availabilityService.GetAvailabilitiesByProfessionalAsync(professionalId);
            return Results.Ok(availabilities);
        })
        .WithName("GetAvailabilitiesByProfessional")
        .WithOpenApi();

        // Get available slots for a specific date
        group.MapGet("/slots/{professionalId}", async (
            Guid professionalId,
            [FromQuery] DateTime date,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var slots = await availabilityService.GetAvailableSlotsAsync(professionalId, date);
            return Results.Ok(slots);
        })
        .WithName("GetAvailableSlots")
        .WithOpenApi();

        // Generate slots for a date
        group.MapPost("/slots/generate/{professionalId}", async (
            Guid professionalId,
            [FromQuery] DateTime date,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var slots = await availabilityService.GenerateSlotsForDateAsync(professionalId, date);
            return Results.Ok(slots);
        })
        .WithName("GenerateSlots")
        .WithOpenApi();

        // Update availability
        group.MapPut("/{id}", async (
            Guid id,
            [FromBody] UpdateAvailabilityDto dto,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var availability = await availabilityService.UpdateAvailabilityAsync(
                id,
                dto.DayOfWeek,
                dto.StartTime,
                dto.EndTime,
                dto.EndDate);
            return Results.Ok(availability);
        })
        .WithName("UpdateAvailability")
        .WithOpenApi();

        // Delete availability
        group.MapDelete("/{id}", async (
            Guid id,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var result = await availabilityService.DeleteAvailabilityAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteAvailability")
        .WithOpenApi();
    }
}

public class UpdateAvailabilityDto
{
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? EndDate { get; set; }
}