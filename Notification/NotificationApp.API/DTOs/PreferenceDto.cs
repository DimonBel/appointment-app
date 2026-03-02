using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO representing user notification preferences
/// </summary>
public record PreferenceDto(
    Guid Id,
    Guid UserId,
    NotificationType NotificationType,
    bool InAppEnabled,
    bool EmailEnabled,
    bool PushEnabled
);