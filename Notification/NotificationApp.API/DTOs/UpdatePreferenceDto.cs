using System.ComponentModel.DataAnnotations;
using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO for updating user notification preferences
/// </summary>
public record UpdatePreferenceDto(
    [Required] NotificationType NotificationType,
    bool InAppEnabled = true,
    bool EmailEnabled = false,
    bool PushEnabled = false
);