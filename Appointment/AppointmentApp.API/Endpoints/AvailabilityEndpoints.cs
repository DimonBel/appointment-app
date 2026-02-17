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

            var responseDto = MapToAvailabilityResponseDto(availability);
            return Results.Created($"/api/availabilities/{availability.Id}", responseDto);
        })
        .WithName("CreateAvailability")
        .WithOpenApi();

        // Get availability by ID
        group.MapGet("/{id}", async (
            Guid id,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var availability = await availabilityService.GetAvailabilityByIdAsync(id);
            if (availability == null) return Results.NotFound();
            var responseDto = MapToAvailabilityResponseDto(availability);
            return Results.Ok(responseDto);
        })
        .WithName("GetAvailabilityById")
        .WithOpenApi();

        // Get availabilities by professional
        group.MapGet("/professional/{professionalId}", async (
            Guid professionalId,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var availabilities = await availabilityService.GetAvailabilitiesByProfessionalAsync(professionalId);
            var responseDtos = availabilities.Select(MapToAvailabilityResponseDto);
            return Results.Ok(responseDtos);
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
            var slotDtos = slots.Select(MapToAvailabilitySlotDto);
            return Results.Ok(slotDtos);
        })
        .WithName("GetAvailableSlots")
        .WithOpenApi();

        // Get all slots with status (available and occupied) for a specific date
        group.MapGet("/slots/status/{professionalId}", async (
            Guid professionalId,
            [FromQuery] DateTime date,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var slots = await availabilityService.GetSlotsByDateAsync(professionalId, date);
            var slotDtos = slots.Select(MapToAvailabilitySlotDto);
            return Results.Ok(slotDtos);
        })
        .WithName("GetSlotsWithStatus")
        .WithOpenApi();

        // Generate slots for a date
        group.MapPost("/slots/generate/{professionalId}", async (
            Guid professionalId,
            [FromQuery] DateTime date,
            [FromServices] IAvailabilityService availabilityService) =>
        {
            var slots = await availabilityService.GenerateSlotsForDateAsync(professionalId, date);
            var slotDtos = slots.Select(MapToAvailabilitySlotDto);
            return Results.Ok(slotDtos);
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
            var responseDto = MapToAvailabilityResponseDto(availability);
            return Results.Ok(responseDto);
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

    private static AvailabilityResponseDto MapToAvailabilityResponseDto(Availability availability)
    {
        return new AvailabilityResponseDto
        {
            Id = availability.Id,
            ProfessionalId = availability.ProfessionalId,
            DayOfWeek = (int)availability.DayOfWeek,
            StartTime = availability.StartTime.ToString(@"hh\:mm"),
            EndTime = availability.EndTime.ToString(@"hh\:mm"),
            ScheduleType = (int)availability.ScheduleType,
            StartDate = availability.StartDate,
            EndDate = availability.EndDate,
            IsActive = availability.IsActive,
            CreatedAt = availability.CreatedAt
        };
    }

    private static AvailabilitySlotDto MapToAvailabilitySlotDto(AvailabilitySlot slot)
    {
        return new AvailabilitySlotDto
        {
            Id = slot.Id,
            AvailabilityId = slot.AvailabilityId,
            SlotDate = slot.SlotDate,
            StartTime = slot.StartTime.ToString(@"hh\:mm"),
            EndTime = slot.EndTime.ToString(@"hh\:mm"),
            IsAvailable = slot.IsAvailable,
            CreatedAt = slot.CreatedAt
        };
    }
}

public class UpdateAvailabilityDto
{
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? EndDate { get; set; }
}