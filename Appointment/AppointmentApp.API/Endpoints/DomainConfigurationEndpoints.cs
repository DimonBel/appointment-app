using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.API.Endpoints;

public static class DomainConfigurationEndpoints
{
    public static void MapDomainConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/domain-configurations")
            .WithTags("DomainConfigurations")
            .RequireAuthorization();

        // Create domain configuration
        group.MapPost("/", async (
            [FromBody] CreateDomainConfigurationDto dto,
            [FromServices] IDomainConfigurationService domainConfigurationService) =>
        {
            var config = await domainConfigurationService.CreateDomainConfigurationAsync(
                dto.DomainType,
                dto.Name,
                dto.Description,
                dto.DefaultDurationMinutes);

            if (dto.RequiredFields != null)
            {
                config.RequiredFields = dto.RequiredFields;
            }

            return Results.Created($"/api/domain-configurations/{config.Id}", config);
        })
        .WithName("CreateDomainConfiguration")
        .WithOpenApi();

        // Get domain configuration by ID
        group.MapGet("/{id}", async (
            Guid id,
            [FromServices] IDomainConfigurationService domainConfigurationService) =>
        {
            var config = await domainConfigurationService.GetDomainConfigurationByIdAsync(id);
            return config != null ? Results.Ok(config) : Results.NotFound();
        })
        .WithName("GetDomainConfigurationById")
        .WithOpenApi();

        // Get all domain configurations
        group.MapGet("/", async (
            [FromServices] IDomainConfigurationService domainConfigurationService,
            [FromQuery] bool onlyActive = true) =>
        {
            var configs = await domainConfigurationService.GetAllDomainConfigurationsAsync(onlyActive);
            return Results.Ok(configs);
        })
        .WithName("GetAllDomainConfigurations")
        .WithOpenApi();

        // Update domain configuration
        group.MapPut("/{id}", async (
            Guid id,
            [FromBody] UpdateDomainConfigurationDto dto,
            [FromServices] IDomainConfigurationService domainConfigurationService) =>
        {
            var config = await domainConfigurationService.UpdateDomainConfigurationAsync(
                id,
                dto.Name,
                dto.Description,
                dto.DefaultDurationMinutes);
            return Results.Ok(config);
        })
        .WithName("UpdateDomainConfiguration")
        .WithOpenApi();

        // Activate domain configuration
        group.MapPost("/{id}/activate", async (
            Guid id,
            [FromServices] IDomainConfigurationService domainConfigurationService) =>
        {
            await domainConfigurationService.ActivateDomainConfigurationAsync(id);
            return Results.NoContent();
        })
        .WithName("ActivateDomainConfiguration")
        .WithOpenApi();

        // Deactivate domain configuration
        group.MapPost("/{id}/deactivate", async (
            Guid id,
            [FromServices] IDomainConfigurationService domainConfigurationService) =>
        {
            await domainConfigurationService.DeactivateDomainConfigurationAsync(id);
            return Results.NoContent();
        })
        .WithName("DeactivateDomainConfiguration")
        .WithOpenApi();

        // Delete domain configuration
        group.MapDelete("/{id}", async (
            Guid id,
            [FromServices] IDomainConfigurationService domainConfigurationService) =>
        {
            var result = await domainConfigurationService.DeleteDomainConfigurationAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteDomainConfiguration")
        .WithOpenApi();
    }
}

public class UpdateDomainConfigurationDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? DefaultDurationMinutes { get; set; }
}