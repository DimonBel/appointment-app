using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.API.Endpoints;

public static class ProfessionalEndpoints
{
    public static void MapProfessionalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professionals")
            .WithTags("Professionals");
            // .RequireAuthorization(); // Temporarily disabled for testing

        // Create professional
        group.MapPost("/", async (
            [FromBody] CreateProfessionalDto dto,
            [FromServices] IProfessionalService professionalService) =>
        {
            var professional = await professionalService.CreateProfessionalAsync(
                dto.UserId,
                dto.Title,
                dto.Qualifications,
                dto.Specialization);

            if (dto.HourlyRate.HasValue || dto.ExperienceYears.HasValue || dto.Bio != null)
            {
                professional = await professionalService.UpdateProfessionalAsync(
                    professional.Id,
                    dto.Title,
                    dto.Qualifications,
                    dto.Specialization,
                    dto.HourlyRate,
                    dto.ExperienceYears,
                    dto.Bio);
            }

            return Results.Created($"/api/professionals/{professional.Id}", professional);
        })
        .WithName("CreateProfessional")
        .WithOpenApi();

        // Get professional by ID
        group.MapGet("/{id}", async (
            Guid id,
            [FromServices] IProfessionalService professionalService) =>
        {
            var professional = await professionalService.GetProfessionalByIdAsync(id);
            return professional != null ? Results.Ok(professional) : Results.NotFound();
        })
        .WithName("GetProfessionalById")
        .WithOpenApi();

        // Get professional by user ID
        group.MapGet("/user/{userId}", async (
            Guid userId,
            [FromServices] IProfessionalService professionalService) =>
        {
            var professional = await professionalService.GetProfessionalByUserIdAsync(userId);
            return professional != null ? Results.Ok(professional) : Results.NotFound();
        })
        .WithName("GetProfessionalByUserId")
        .WithOpenApi();

        // Get all professionals
        group.MapGet("/", async (
            [FromServices] IProfessionalService professionalService,
            [FromQuery] bool onlyAvailable = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var professionals = await professionalService.GetAllProfessionalsAsync(onlyAvailable, page, pageSize);
            return Results.Ok(professionals);
        })
        .WithName("GetAllProfessionals")
        .WithOpenApi();

        // Update professional
        group.MapPut("/{id}", async (
            Guid id,
            [FromBody] UpdateProfessionalDto dto,
            [FromServices] IProfessionalService professionalService) =>
        {
            var professional = await professionalService.UpdateProfessionalAsync(
                id,
                dto.Title,
                dto.Qualifications,
                dto.Specialization,
                dto.HourlyRate,
                dto.ExperienceYears,
                dto.Bio);
            return Results.Ok(professional);
        })
        .WithName("UpdateProfessional")
        .WithOpenApi();

        // Set professional availability
        group.MapPatch("/{id}/availability", async (
            Guid id,
            [FromBody] SetAvailabilityDto dto,
            [FromServices] IProfessionalService professionalService) =>
        {
            await professionalService.SetProfessionalAvailabilityAsync(id, dto.IsAvailable);
            return Results.NoContent();
        })
        .WithName("SetProfessionalAvailability")
        .WithOpenApi();

        // Delete professional
        group.MapDelete("/{id}", async (
            Guid id,
            [FromServices] IProfessionalService professionalService) =>
        {
            var result = await professionalService.DeleteProfessionalAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProfessional")
        .WithOpenApi();
    }
}

public class UpdateProfessionalDto
{
    public string? Title { get; set; }
    public string? Qualifications { get; set; }
    public string? Specialization { get; set; }
    public decimal? HourlyRate { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Bio { get; set; }
}

public class SetAvailabilityDto
{
    public bool IsAvailable { get; set; }
}