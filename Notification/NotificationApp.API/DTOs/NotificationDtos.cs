using NotificationApp.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NotificationApp.API.DTOs;

public record CreateNotificationDto(
    [Required] Guid UserId,
    [Required][MaxLength(500)] string Title,
    [Required][MaxLength(2000)] string Message,
    [Required] NotificationType Type,
    Guid? ReferenceId = null,
    string? ReferenceType = null,
    string? Metadata = null
);

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

public record UpdatePreferenceDto(
    [Required] NotificationType NotificationType,
    bool InAppEnabled = true,
    bool EmailEnabled = false,
    bool PushEnabled = false
);

public record PreferenceDto(
    Guid Id,
    Guid UserId,
    NotificationType NotificationType,
    bool InAppEnabled,
    bool EmailEnabled,
    bool PushEnabled
);

public record CreateTemplateDto(
    [Required][MaxLength(200)] string Key,
    [Required][MaxLength(300)] string Name,
    [Required][MaxLength(500)] string TitleTemplate,
    [Required][MaxLength(4000)] string BodyTemplate,
    [Required] NotificationType Type
);

public record UpdateTemplateDto(
    [MaxLength(300)] string? Name,
    [MaxLength(500)] string? TitleTemplate,
    [MaxLength(4000)] string? BodyTemplate,
    bool? IsActive
);

public record TemplateDto(
    Guid Id,
    string Key,
    string Name,
    string TitleTemplate,
    string BodyTemplate,
    NotificationType Type,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateScheduleDto(
    [Required] Guid UserId,
    [Required] NotificationType NotificationType,
    [Required] DateTime ScheduledAt,
    Guid? ReferenceId = null,
    string? ReferenceType = null,
    Guid? TemplateId = null,
    string? TemplateData = null
);

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

public record EventDto(
    [Required] string SourceService,
    [Required] string EventName,
    [Required] string Payload
);

public record NotificationEventDto(
    Guid Id,
    string SourceService,
    string EventName,
    bool IsProcessed,
    int RetryCount,
    string? ErrorMessage,
    DateTime ReceivedAt,
    DateTime? ProcessedAt
);
