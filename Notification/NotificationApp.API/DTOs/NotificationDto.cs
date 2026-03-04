using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO representing a notification
/// </summary>
public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    NotificationType Type,
    NotificationChannel Channel,
    NotificationStatus Status,
    NotificationPriority Priority,
    Guid? ReferenceId,
    string? ReferenceType,
    string? Metadata,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? ReadAt
);