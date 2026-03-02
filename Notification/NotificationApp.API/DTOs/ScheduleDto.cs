using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO representing a scheduled notification
/// </summary>
public record ScheduleDto(
    Guid Id,
    Guid UserId,
    NotificationType NotificationType,
    DateTime ScheduledAt,
    bool IsProcessed,
    bool IsCancelled,
    Guid? ReferenceId,
    string? ReferenceType,
    DateTime CreatedAt
);