using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO representing a notification template
/// </summary>
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