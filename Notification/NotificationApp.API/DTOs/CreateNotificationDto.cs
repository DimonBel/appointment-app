using System.ComponentModel.DataAnnotations;
using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO for creating a new notification
/// </summary>
public record CreateNotificationDto(
    [Required] Guid UserId,
    [Required][MaxLength(500)] string Title,
    [Required][MaxLength(2000)] string Message,
    [Required] NotificationType Type,
    Guid? ReferenceId = null,
    string? ReferenceType = null,
    string? Metadata = null
);