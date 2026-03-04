using System.ComponentModel.DataAnnotations;
using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO for creating a scheduled notification
/// </summary>
public record CreateScheduleDto(
    [Required] Guid UserId,
    [Required] NotificationType NotificationType,
    [Required] DateTime ScheduledAt,
    Guid? ReferenceId = null,
    string? ReferenceType = null,
    Guid? TemplateId = null,
    string? TemplateData = null
);