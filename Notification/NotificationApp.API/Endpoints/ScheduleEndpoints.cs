using NotificationApp.API.DTOs;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Interfaces;

namespace NotificationApp.API.Endpoints;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications/schedules").WithTags("Notification Schedules");

        // GET /api/notifications/schedules?userId={userId}
        group.MapGet("/", async (Guid userId, INotificationScheduleService service) =>
        {
            var schedules = await service.GetByUserIdAsync(userId);
            return Results.Ok(schedules.Select(MapToDto));
        }).WithName("GetSchedules");

        // GET /api/notifications/schedules/pending
        group.MapGet("/pending", async (INotificationScheduleService service) =>
        {
            var pending = await service.GetPendingSchedulesAsync();
            return Results.Ok(pending.Select(MapToDto));
        }).WithName("GetPendingSchedules");

        // POST /api/notifications/schedules
        group.MapPost("/", async (CreateScheduleDto dto, INotificationScheduleService service) =>
        {
            var schedule = new NotificationSchedule
            {
                UserId = dto.UserId,
                NotificationType = dto.NotificationType,
                ScheduledAt = dto.ScheduledAt,
                ReferenceId = dto.ReferenceId,
                ReferenceType = dto.ReferenceType,
                TemplateId = dto.TemplateId,
                TemplateData = dto.TemplateData
            };

            var result = await service.CreateAsync(schedule);
            return Results.Created($"/api/notifications/schedules/{result.Id}", MapToDto(result));
        }).WithName("CreateSchedule");

        // POST /api/notifications/schedules/reminder
        group.MapPost("/reminder", async (
            Guid userId, Guid orderId, DateTime appointmentTime,
            INotificationScheduleService service,
            int minutesBefore = 60) =>
        {
            await service.ScheduleAppointmentReminderAsync(userId, orderId, appointmentTime, minutesBefore);
            return Results.Ok();
        }).WithName("ScheduleReminder");

        // PUT /api/notifications/schedules/{id}/cancel
        group.MapPut("/{id:guid}/cancel", async (Guid id, INotificationScheduleService service) =>
        {
            await service.CancelAsync(id);
            return Results.Ok();
        }).WithName("CancelSchedule");

        // POST /api/notifications/schedules/process
        group.MapPost("/process", async (INotificationScheduleService service) =>
        {
            await service.ProcessPendingSchedulesAsync();
            return Results.Ok();
        }).WithName("ProcessPendingSchedules");
    }

    private static ScheduleDto MapToDto(NotificationSchedule s) => new(
        s.Id, s.UserId, s.NotificationType, s.ScheduledAt,
        s.IsProcessed, s.IsCancelled, s.ReferenceId, s.ReferenceType, s.CreatedAt
    );
}
