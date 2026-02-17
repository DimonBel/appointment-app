using NotificationApp.API.DTOs;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;

namespace NotificationApp.API.Endpoints;

public static class PreferenceEndpoints
{
    public static void MapPreferenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications/preferences").WithTags("Notification Preferences");

        // GET /api/notifications/preferences?userId={userId}
        group.MapGet("/", async (Guid userId, INotificationPreferenceService service) =>
        {
            var preferences = await service.GetByUserIdAsync(userId);
            var dtos = preferences.Select(p => new PreferenceDto(
                p.Id, p.UserId, p.NotificationType, p.InAppEnabled, p.EmailEnabled, p.PushEnabled));
            return Results.Ok(dtos);
        }).WithName("GetPreferences");

        // PUT /api/notifications/preferences?userId={userId}
        group.MapPut("/", async (Guid userId, UpdatePreferenceDto dto, INotificationPreferenceService service) =>
        {
            var preference = new NotificationPreference
            {
                UserId = userId,
                NotificationType = dto.NotificationType,
                InAppEnabled = dto.InAppEnabled,
                EmailEnabled = dto.EmailEnabled,
                PushEnabled = dto.PushEnabled
            };

            var result = await service.CreateOrUpdateAsync(preference);
            return Results.Ok(new PreferenceDto(
                result.Id, result.UserId, result.NotificationType,
                result.InAppEnabled, result.EmailEnabled, result.PushEnabled));
        }).WithName("UpdatePreference");

        // POST /api/notifications/preferences/defaults?userId={userId}
        group.MapPost("/defaults", async (Guid userId, INotificationPreferenceService service) =>
        {
            await service.SetDefaultPreferencesAsync(userId);
            return Results.Ok();
        }).WithName("SetDefaultPreferences");
    }
}
