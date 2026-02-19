using NotificationApp.API.DTOs;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;
using System.Security.Claims;

namespace NotificationApp.API.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        // GET /api/notifications?userId={userId}&page=1&pageSize=20
        group.MapGet("/", async (
            Guid? userId,
            HttpContext httpContext,
            INotificationService service,
            int page = 1,
            int pageSize = 20) =>
        {
            var resolvedUserId = ResolveUserId(httpContext, userId);
            if (!resolvedUserId.HasValue) return Results.BadRequest(new { error = "userId is required." });

            var notifications = await service.GetByUserIdAsync(resolvedUserId.Value, page, pageSize);
            var dtos = notifications.Select(MapToDto);
            return Results.Ok(dtos);
        }).WithName("GetNotifications");

        // GET /api/notifications/{id}
        group.MapGet("/{id:guid}", async (Guid id, INotificationService service) =>
        {
            var notification = await service.GetByIdAsync(id);
            return notification is null
                ? Results.NotFound()
                : Results.Ok(MapToDto(notification));
        }).WithName("GetNotificationById");

        // GET /api/notifications/unread-count?userId={userId}
        group.MapGet("/unread-count", async (Guid? userId, HttpContext httpContext, INotificationService service) =>
        {
            var resolvedUserId = ResolveUserId(httpContext, userId);
            if (!resolvedUserId.HasValue) return Results.BadRequest(new { error = "userId is required." });

            var count = await service.GetUnreadCountAsync(resolvedUserId.Value);
            return Results.Ok(new { count });
        }).WithName("GetUnreadCount");

        // GET /api/notifications/unread?userId={userId}
        group.MapGet("/unread", async (Guid? userId, HttpContext httpContext, INotificationService service, int page = 1, int pageSize = 20) =>
        {
            var resolvedUserId = ResolveUserId(httpContext, userId);
            if (!resolvedUserId.HasValue) return Results.BadRequest(new { error = "userId is required." });

            var notifications = await service.GetUnreadByUserIdAsync(resolvedUserId.Value, page, pageSize);
            var dtos = notifications.Select(MapToDto);
            return Results.Ok(dtos);
        }).WithName("GetUnreadNotifications");

        // POST /api/notifications
        group.MapPost("/", async (CreateNotificationDto dto, INotificationService service) =>
        {
            await service.SendNotificationAsync(
                dto.UserId, dto.Type, dto.Title, dto.Message,
                dto.ReferenceId, dto.ReferenceType, dto.Metadata);
            return Results.Created();
        }).WithName("CreateNotification");

        // PUT /api/notifications/{id}/read
        group.MapPut("/{id:guid}/read", async (Guid id, INotificationService service) =>
        {
            await service.MarkAsReadAsync(id);
            return Results.Ok();
        }).WithName("MarkNotificationAsRead");

        // PUT /api/notifications/read-all?userId={userId}
        group.MapPut("/read-all", async (Guid? userId, HttpContext httpContext, INotificationService service) =>
        {
            var resolvedUserId = ResolveUserId(httpContext, userId);
            if (!resolvedUserId.HasValue) return Results.BadRequest(new { error = "userId is required." });

            await service.MarkAllAsReadAsync(resolvedUserId.Value);
            return Results.Ok();
        }).WithName("MarkAllNotificationsAsRead");

        // DELETE /api/notifications/{id}
        group.MapDelete("/{id:guid}", async (Guid id, INotificationService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        }).WithName("DeleteNotification");

        // GET /api/notifications/by-type?userId={userId}&type={type}
        group.MapGet("/by-type", async (Guid? userId, HttpContext httpContext, NotificationType type, INotificationService service) =>
        {
            var resolvedUserId = ResolveUserId(httpContext, userId);
            if (!resolvedUserId.HasValue) return Results.BadRequest(new { error = "userId is required." });

            var notifications = await service.GetByTypeAsync(resolvedUserId.Value, type);
            return Results.Ok(notifications.Select(MapToDto));
        }).WithName("GetNotificationsByType");
    }

    private static Guid? ResolveUserId(HttpContext context, Guid? queryUserId)
    {
        if (queryUserId.HasValue)
        {
            return queryUserId;
        }

        var claimValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue("nameid");

        if (Guid.TryParse(claimValue, out var fromClaim))
        {
            return fromClaim;
        }

        if (context.Request.Headers.TryGetValue("X-User-Id", out var headerValues)
            && Guid.TryParse(headerValues.FirstOrDefault(), out var fromHeader))
        {
            return fromHeader;
        }

        return null;
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id, n.UserId, n.Title, n.Message, n.Type, n.Channel,
        n.Status, n.Priority, n.ReferenceId, n.ReferenceType,
        n.Metadata, n.CreatedAt, n.SentAt, n.ReadAt
    );
}
