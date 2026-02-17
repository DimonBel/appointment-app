using Microsoft.Extensions.Logging;
using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;
using NotificationApp.Domain.Interfaces;
using NotificationApp.Repository.Interfaces;
using System.Text.Json;

namespace NotificationApp.Service.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IEmailService _emailService;
    private readonly IRealTimeNotifier _realTimeNotifier;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        INotificationPreferenceService preferenceService,
        IEmailService emailService,
        IRealTimeNotifier realTimeNotifier,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _preferenceService = preferenceService;
        _emailService = emailService;
        _realTimeNotifier = realTimeNotifier;
        _logger = logger;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTime.UtcNow;
        return await _notificationRepository.CreateAsync(notification);
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _notificationRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _notificationRepository.GetByUserIdAsync(userId, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification != null)
        {
            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.UtcNow;
            await _notificationRepository.UpdateAsync(notification);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await _notificationRepository.MarkAllAsReadAsync(userId);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _notificationRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<Notification>> GetByTypeAsync(Guid userId, NotificationType type)
    {
        return await _notificationRepository.GetByTypeAsync(userId, type);
    }

    public async Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message,
        Guid? referenceId = null, string? referenceType = null, string? metadata = null)
    {
        // Always create InApp notification
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Channel = NotificationChannel.InApp,
            Status = NotificationStatus.Sent,
            Priority = GetDefaultPriority(type),
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            Metadata = metadata,
            SentAt = DateTime.UtcNow
        };

        await _notificationRepository.CreateAsync(notification);

        // Send real-time notification via SignalR
        try
        {
            await _realTimeNotifier.SendToUserAsync(userId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send real-time notification to user {UserId}", userId);
        }

        // Try to send email notification (only for booking confirmation)
        if (type != NotificationType.BookingConfirmation)
        {
            return;
        }

        string? userEmail = null;
        if (metadata != null)
        {
            try
            {
                var meta = JsonSerializer.Deserialize<JsonElement>(metadata);
                if (meta.TryGetProperty("email", out var emailProp))
                    userEmail = emailProp.GetString();
            }
            catch { /* metadata is not JSON or doesn't contain email */ }
        }

        if (!string.IsNullOrEmpty(userEmail))
        {
            var emailNotification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Channel = NotificationChannel.Email,
                Status = NotificationStatus.Pending,
                Priority = GetDefaultPriority(type),
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                Metadata = metadata
            };
            await _notificationRepository.CreateAsync(emailNotification);

            // Actually send the email
            var emailSent = await _emailService.SendEmailAsync(userEmail, title, message);

            emailNotification.Status = emailSent ? NotificationStatus.Sent : NotificationStatus.Failed;
            emailNotification.SentAt = emailSent ? DateTime.UtcNow : null;
            await _notificationRepository.UpdateAsync(emailNotification);

            _logger.LogInformation("Email notification {Status} for user {UserId} to {Email}",
                emailSent ? "sent" : "failed", userId, userEmail);
        }
    }

    private static NotificationPriority GetDefaultPriority(NotificationType type)
    {
        return type switch
        {
            NotificationType.OrderCreated => NotificationPriority.High,
            NotificationType.OrderApproved => NotificationPriority.High,
            NotificationType.OrderDeclined => NotificationPriority.High,
            NotificationType.OrderCancelled => NotificationPriority.High,
            NotificationType.OrderReminder => NotificationPriority.Urgent,
            NotificationType.SystemAlert => NotificationPriority.Urgent,
            _ => NotificationPriority.Normal
        };
    }
}
