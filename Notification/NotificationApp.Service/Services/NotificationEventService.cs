using System.Text.Json;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Service.Services;

/// <summary>
/// Handles events from other microservices and converts them to notifications.
/// Module 2.5 - Event Listener
/// </summary>
public class NotificationEventService : INotificationEventService
{
    private readonly INotificationEventRepository _eventRepository;
    private readonly INotificationService _notificationService;
    private readonly INotificationTemplateService _templateService;
    private readonly INotificationScheduleService _scheduleService;

    public NotificationEventService(
        INotificationEventRepository eventRepository,
        INotificationService notificationService,
        INotificationTemplateService templateService,
        INotificationScheduleService scheduleService)
    {
        _eventRepository = eventRepository;
        _notificationService = notificationService;
        _templateService = templateService;
        _scheduleService = scheduleService;
    }

    public async Task<NotificationEvent> RecordEventAsync(string sourceService, string eventName, string payload)
    {
        var notificationEvent = new NotificationEvent
        {
            SourceService = sourceService,
            EventName = eventName,
            Payload = payload
        };

        return await _eventRepository.CreateAsync(notificationEvent);
    }

    public async Task ProcessEventAsync(Guid eventId)
    {
        var ev = await _eventRepository.GetByIdAsync(eventId);
        if (ev == null || ev.IsProcessed) return;

        try
        {
            await ProcessEventByNameAsync(ev);

            ev.IsProcessed = true;
            ev.ProcessedAt = DateTime.UtcNow;
            await _eventRepository.UpdateAsync(ev);
        }
        catch (Exception ex)
        {
            ev.RetryCount++;
            ev.ErrorMessage = ex.Message;
            await _eventRepository.UpdateAsync(ev);
        }
    }

    public async Task<IEnumerable<NotificationEvent>> GetUnprocessedEventsAsync()
    {
        return await _eventRepository.GetUnprocessedAsync();
    }

    public async Task<IEnumerable<NotificationEvent>> GetFailedEventsAsync()
    {
        return await _eventRepository.GetFailedAsync();
    }

    public async Task RetryFailedEventAsync(Guid eventId)
    {
        var ev = await _eventRepository.GetByIdAsync(eventId);
        if (ev != null && !ev.IsProcessed)
        {
            ev.RetryCount = 0;
            ev.ErrorMessage = null;
            await _eventRepository.UpdateAsync(ev);
            await ProcessEventAsync(eventId);
        }
    }

    private async Task ProcessEventByNameAsync(NotificationEvent ev)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(ev.Payload);

        switch (ev.EventName)
        {
            case "OrderCreated":
                await HandleOrderCreatedAsync(payload);
                break;
            case "OrderApproved":
                await HandleOrderStatusChangeAsync(payload, NotificationType.OrderApproved, "order_approved");
                break;
            case "OrderDeclined":
                await HandleOrderStatusChangeAsync(payload, NotificationType.OrderDeclined, "order_declined");
                break;
            case "OrderCancelled":
                await HandleOrderStatusChangeAsync(payload, NotificationType.OrderCancelled, "order_cancelled");
                break;
            case "OrderCompleted":
                await HandleOrderStatusChangeAsync(payload, NotificationType.OrderCompleted, "order_completed");
                break;
            case "OrderRescheduled":
                await HandleOrderStatusChangeAsync(payload, NotificationType.OrderRescheduled, "order_rescheduled");
                break;
            case "ChatMessageReceived":
                await HandleChatMessageAsync(payload);
                break;
            case "PasswordChanged":
                await HandlePasswordChangedAsync(payload);
                break;
            case "BookingConfirmed":
                await HandleBookingConfirmedAsync(payload);
                break;
            case "FriendRequestSent":
                await HandleFriendRequestAsync(payload);
                break;
            case "FriendRequestAccepted":
                await HandleFriendRequestAcceptedAsync(payload);
                break;
            case "FriendRequestDeclined":
                await HandleFriendRequestDeclinedAsync(payload);
                break;
            default:
                // Unknown event - mark as processed anyway
                break;
        }
    }

    private async Task HandleOrderCreatedAsync(JsonElement payload)
    {
        var professionalId = payload.GetProperty("professionalId").GetGuid();
        var orderId = payload.GetProperty("orderId").GetGuid();
        var patientName = payload.TryGetProperty("patientName", out var pn) ? pn.GetString() ?? "Patient" : "Patient";
        var appointmentDate = payload.TryGetProperty("appointmentDate", out var ad) ? ad.GetString() ?? "" : "";
        var appointmentTime = payload.TryGetProperty("appointmentTime", out var at) ? at.GetString() ?? "" : "";

        var data = new Dictionary<string, string>
        {
            ["PatientName"] = patientName,
            ["AppointmentDate"] = appointmentDate,
            ["AppointmentTime"] = appointmentTime
        };

        var metadata = JsonSerializer.Serialize(new
        {
            action = "booking_request",
            orderId,
            patientName,
            appointmentDate,
            appointmentTime
        });

        try
        {
            var (title, body) = await _templateService.RenderTemplateAsync("order_created", data);
            await _notificationService.SendNotificationAsync(
                professionalId, NotificationType.OrderCreated, title, body, orderId, "Order", metadata);
        }
        catch
        {
            await _notificationService.SendNotificationAsync(
                professionalId, NotificationType.OrderCreated,
                "New Appointment Request",
                $"You have a new appointment request from {patientName}.",
                orderId, "Order", metadata);
        }

        // Schedule reminder for the appointment
        if (payload.TryGetProperty("scheduledDateTime", out var sdt))
        {
            var scheduledTime = sdt.GetDateTime();
            // Reminder for professional
            await _scheduleService.ScheduleAppointmentReminderAsync(professionalId, orderId, scheduledTime, 60);

            // Reminder for client
            if (payload.TryGetProperty("clientId", out var ci))
            {
                var clientId = ci.GetGuid();
                await _scheduleService.ScheduleAppointmentReminderAsync(clientId, orderId, scheduledTime, 60);
            }
        }
    }

    private async Task HandleOrderStatusChangeAsync(JsonElement payload, NotificationType type, string templateKey)
    {
        var userId = payload.GetProperty("userId").GetGuid();
        var orderId = payload.GetProperty("orderId").GetGuid();
        var doctorName = payload.TryGetProperty("doctorName", out var dn) ? dn.GetString() ?? "Doctor" : "Doctor";
        var appointmentDate = payload.TryGetProperty("appointmentDate", out var ad) ? ad.GetString() ?? "" : "";
        var appointmentTime = payload.TryGetProperty("appointmentTime", out var at) ? at.GetString() ?? "" : "";
        var reason = payload.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
        var email = payload.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        var metadata = string.IsNullOrWhiteSpace(email)
            ? null
            : JsonSerializer.Serialize(new { email });

        var data = new Dictionary<string, string>
        {
            ["DoctorName"] = doctorName,
            ["AppointmentDate"] = appointmentDate,
            ["AppointmentTime"] = appointmentTime,
            ["Reason"] = reason
        };

        try
        {
            var (title, body) = await _templateService.RenderTemplateAsync(templateKey, data);
            await _notificationService.SendNotificationAsync(userId, type, title, body, orderId, "Order", metadata);
        }
        catch
        {
            await _notificationService.SendNotificationAsync(
                userId, type, $"Appointment {type}", $"Your appointment status has been updated.", orderId, "Order", metadata);
        }
    }

    private async Task HandleChatMessageAsync(JsonElement payload)
    {
        var receiverId = payload.GetProperty("receiverId").GetGuid();
        var senderName = payload.TryGetProperty("senderName", out var sn) ? sn.GetString() ?? "Someone" : "Someone";

        var data = new Dictionary<string, string>
        {
            ["SenderName"] = senderName
        };

        try
        {
            var (title, body) = await _templateService.RenderTemplateAsync("chat_message", data);
            await _notificationService.SendNotificationAsync(
                receiverId, NotificationType.ChatMessage, title, body);
        }
        catch
        {
            await _notificationService.SendNotificationAsync(
                receiverId, NotificationType.ChatMessage,
                "New Message", $"You have a new message from {senderName}.");
        }
    }

    private async Task HandlePasswordChangedAsync(JsonElement payload)
    {
        var userId = payload.GetProperty("userId").GetGuid();
        var email = payload.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        var userName = payload.TryGetProperty("userName", out var un) ? un.GetString() ?? "User" : "User";

        var metadata = JsonSerializer.Serialize(new { email });

        await _notificationService.SendNotificationAsync(
            userId, NotificationType.PasswordChanged,
            "Password Changed Successfully",
            $@"<h2>Hello {userName},</h2>
            <p>Your password has been changed successfully.</p>
            <p>If you did not make this change, please contact support immediately.</p>
            <p><strong>Changed at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>",
            metadata: metadata);
    }

    private async Task HandleBookingConfirmedAsync(JsonElement payload)
    {
        var userId = payload.GetProperty("userId").GetGuid();
        var email = payload.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
        var doctorName = payload.TryGetProperty("doctorName", out var dn) ? dn.GetString() ?? "Doctor" : "Doctor";
        var appointmentDate = payload.TryGetProperty("appointmentDate", out var ad) ? ad.GetString() ?? "" : "";
        var appointmentTime = payload.TryGetProperty("appointmentTime", out var at) ? at.GetString() ?? "" : "";
        var orderId = payload.TryGetProperty("orderId", out var oi) ? oi.GetGuid() : (Guid?)null;

        var metadata = JsonSerializer.Serialize(new { email });

        await _notificationService.SendNotificationAsync(
            userId, NotificationType.BookingConfirmation,
            "Booking Confirmed",
            $@"Your Appointment is Confirmed!
Your appointment has been successfully booked.

Doctor: {doctorName}
Date: {appointmentDate}
Time: {appointmentTime}

Please arrive 10 minutes before your scheduled appointment.",
            orderId, "Order", metadata);
    }

    private async Task HandleFriendRequestAsync(JsonElement payload)
    {
        var receiverId = payload.GetProperty("receiverId").GetGuid();
        var senderName = payload.TryGetProperty("senderName", out var sn) ? sn.GetString() ?? "Someone" : "Someone";
        var senderId = payload.TryGetProperty("senderId", out var si) ? si.GetGuid() : (Guid?)null;
        var friendshipId = payload.TryGetProperty("friendshipId", out var fi) ? fi.GetGuid() : (Guid?)null;

        var metadata = JsonSerializer.Serialize(new { senderId, friendshipId, action = "friend_request" });

        await _notificationService.SendNotificationAsync(
            receiverId, NotificationType.FriendRequest,
            "New Friend Request",
            $"{senderName} sent you a friend request.",
            friendshipId, "Friendship", metadata);
    }

    private async Task HandleFriendRequestAcceptedAsync(JsonElement payload)
    {
        var requesterId = payload.GetProperty("requesterId").GetGuid();
        var accepterName = payload.TryGetProperty("accepterName", out var an) ? an.GetString() ?? "Someone" : "Someone";
        var friendshipId = payload.TryGetProperty("friendshipId", out var fi) ? fi.GetGuid() : (Guid?)null;

        await _notificationService.SendNotificationAsync(
            requesterId, NotificationType.FriendRequestAccepted,
            "Friend Request Accepted",
            $"{accepterName} accepted your friend request. You can now start chatting!",
            friendshipId, "Friendship");
    }

    private async Task HandleFriendRequestDeclinedAsync(JsonElement payload)
    {
        var requesterId = payload.GetProperty("requesterId").GetGuid();
        var declinerName = payload.TryGetProperty("declinerName", out var dn) ? dn.GetString() ?? "Someone" : "Someone";

        await _notificationService.SendNotificationAsync(
            requesterId, NotificationType.FriendRequestDeclined,
            "Friend Request Declined",
            $"{declinerName} declined your friend request.");
    }
}
