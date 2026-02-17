using NotificationApp.API.DTOs;
using NotificationApp.Domain.Interfaces;

namespace NotificationApp.API.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications/events").WithTags("Notification Events");

        // POST /api/notifications/events — receive event from other microservices
        group.MapPost("/", async (EventDto dto, INotificationEventService service) =>
        {
            var ev = await service.RecordEventAsync(dto.SourceService, dto.EventName, dto.Payload);
            // Process immediately
            await service.ProcessEventAsync(ev.Id);
            return Results.Ok(new NotificationEventDto(
                ev.Id, ev.SourceService, ev.EventName, ev.IsProcessed,
                ev.RetryCount, ev.ErrorMessage, ev.ReceivedAt, ev.ProcessedAt));
        }).WithName("ReceiveEvent");

        // GET /api/notifications/events/unprocessed
        group.MapGet("/unprocessed", async (INotificationEventService service) =>
        {
            var events = await service.GetUnprocessedEventsAsync();
            return Results.Ok(events.Select(e => new NotificationEventDto(
                e.Id, e.SourceService, e.EventName, e.IsProcessed,
                e.RetryCount, e.ErrorMessage, e.ReceivedAt, e.ProcessedAt)));
        }).WithName("GetUnprocessedEvents");

        // GET /api/notifications/events/failed
        group.MapGet("/failed", async (INotificationEventService service) =>
        {
            var events = await service.GetFailedEventsAsync();
            return Results.Ok(events.Select(e => new NotificationEventDto(
                e.Id, e.SourceService, e.EventName, e.IsProcessed,
                e.RetryCount, e.ErrorMessage, e.ReceivedAt, e.ProcessedAt)));
        }).WithName("GetFailedEvents");

        // POST /api/notifications/events/{id}/retry
        group.MapPost("/{id:guid}/retry", async (Guid id, INotificationEventService service) =>
        {
            await service.RetryFailedEventAsync(id);
            return Results.Ok();
        }).WithName("RetryEvent");
    }
}
