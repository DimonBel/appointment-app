using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppointmentApp.API.Endpoints;

public static class ProfessionalEndpoints
{
    public static void MapProfessionalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professionals")
            .WithTags("Professionals");
            // .RequireAuthorization(); // Temporarily disabled for testing

        // Get current user's professional profile
        group.MapGet("/me", async (
            [FromServices] IProfessionalService professionalService,
            HttpContext context) =>
        {
            var userId = GetUserIdFromContext(context);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var professional = await professionalService.GetProfessionalByUserIdAsync(userId.Value);
            if (professional == null) return Results.NotFound();
            var responseDto = MapToProfessionalResponseDto(professional);
            return Results.Ok(responseDto);
        })
        .WithName("GetMyProfessionalProfile")
        .WithOpenApi();

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

            var responseDto = MapToProfessionalResponseDto(professional);
            return Results.Created($"/api/professionals/{professional.Id}", responseDto);
        })
        .WithName("CreateProfessional")
        .WithOpenApi();

        // Get professional by ID
        group.MapGet("/{id}", async (
            Guid id,
            [FromServices] IProfessionalService professionalService) =>
        {
            var professional = await professionalService.GetProfessionalByIdAsync(id);
            if (professional == null) return Results.NotFound();
            var responseDto = MapToProfessionalResponseDto(professional);
            return Results.Ok(responseDto);
        })
        .WithName("GetProfessionalById")
        .WithOpenApi();

        // Get professional by user ID
        group.MapGet("/user/{userId}", async (
            Guid userId,
            [FromServices] IProfessionalService professionalService) =>
        {
            var professional = await professionalService.GetProfessionalByUserIdAsync(userId);
            if (professional == null) return Results.NotFound();
            var responseDto = MapToProfessionalResponseDto(professional);
            return Results.Ok(responseDto);
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
            var responseDtos = professionals.Select(MapToProfessionalResponseDto);
            return Results.Ok(responseDtos);
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
            var responseDto = MapToProfessionalResponseDto(professional);
            return Results.Ok(responseDto);
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

    private static Guid? GetUserIdFromContext(HttpContext context)
    {
        var claimValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue("nameid");

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private static ProfessionalResponseDto MapToProfessionalResponseDto(Professional professional)
    {
        return new ProfessionalResponseDto
        {
            Id = professional.Id,
            UserId = professional.UserId,
            Title = professional.Title,
            Qualifications = professional.Qualifications,
            Specialization = professional.Specialization,
            HourlyRate = professional.HourlyRate,
            ExperienceYears = professional.ExperienceYears,
            Bio = professional.Bio,
            IsAvailable = professional.IsAvailable,
            CreatedAt = professional.CreatedAt,
            UpdatedAt = professional.UpdatedAt,
            User = professional.User != null ? new UserInfoDto
            {
                Id = professional.User.Id,
                UserName = professional.User.UserName,
                FirstName = professional.User.FirstName,
                LastName = professional.User.LastName,
                Email = professional.User.Email,
                AvatarUrl = null // AvatarUrl is stored in a different service
            } : null
        };
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