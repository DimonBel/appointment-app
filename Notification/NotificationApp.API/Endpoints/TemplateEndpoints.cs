using NotificationApp.API.DTOs;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;

namespace NotificationApp.API.Endpoints;

public static class TemplateEndpoints
{
    public static void MapTemplateEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications/templates").WithTags("Notification Templates");

        // GET /api/notifications/templates
        group.MapGet("/", async (INotificationTemplateService service) =>
        {
            var templates = await service.GetAllAsync();
            return Results.Ok(templates.Select(MapToDto));
        }).WithName("GetAllTemplates");

        // GET /api/notifications/templates/{id}
        group.MapGet("/{id:guid}", async (Guid id, INotificationTemplateService service) =>
        {
            var template = await service.GetByIdAsync(id);
            return template is null ? Results.NotFound() : Results.Ok(MapToDto(template));
        }).WithName("GetTemplateById");

        // GET /api/notifications/templates/by-key/{key}
        group.MapGet("/by-key/{key}", async (string key, INotificationTemplateService service) =>
        {
            var template = await service.GetByKeyAsync(key);
            return template is null ? Results.NotFound() : Results.Ok(MapToDto(template));
        }).WithName("GetTemplateByKey");

        // POST /api/notifications/templates
        group.MapPost("/", async (CreateTemplateDto dto, INotificationTemplateService service) =>
        {
            var template = new NotificationTemplate
            {
                Key = dto.Key,
                Name = dto.Name,
                TitleTemplate = dto.TitleTemplate,
                BodyTemplate = dto.BodyTemplate,
                Type = dto.Type
            };

            var result = await service.CreateAsync(template);
            return Results.Created($"/api/notifications/templates/{result.Id}", MapToDto(result));
        }).WithName("CreateTemplate");

        // PUT /api/notifications/templates/{id}
        group.MapPut("/{id:guid}", async (Guid id, UpdateTemplateDto dto, INotificationTemplateService service) =>
        {
            var existing = await service.GetByIdAsync(id);
            if (existing is null) return Results.NotFound();

            if (dto.Name != null) existing.Name = dto.Name;
            if (dto.TitleTemplate != null) existing.TitleTemplate = dto.TitleTemplate;
            if (dto.BodyTemplate != null) existing.BodyTemplate = dto.BodyTemplate;
            if (dto.IsActive.HasValue) existing.IsActive = dto.IsActive.Value;

            var result = await service.UpdateAsync(existing);
            return Results.Ok(MapToDto(result));
        }).WithName("UpdateTemplate");

        // DELETE /api/notifications/templates/{id}
        group.MapDelete("/{id:guid}", async (Guid id, INotificationTemplateService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        }).WithName("DeleteTemplate");
    }

    private static TemplateDto MapToDto(NotificationTemplate t) => new(
        t.Id, t.Key, t.Name, t.TitleTemplate, t.BodyTemplate, t.Type, t.IsActive, t.CreatedAt
    );
}
