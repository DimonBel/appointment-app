using System.ComponentModel.DataAnnotations;
using NotificationApp.Domain.Enums;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO for creating a notification template
/// </summary>
public record CreateTemplateDto(
    [Required][MaxLength(200)] string Key,
    [Required][MaxLength(300)] string Name,
    [Required][MaxLength(500)] string TitleTemplate,
    [Required][MaxLength(4000)] string BodyTemplate,
    [Required] NotificationType Type
);