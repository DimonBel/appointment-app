using System.ComponentModel.DataAnnotations;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO for updating a notification template
/// </summary>
public record UpdateTemplateDto(
    [MaxLength(300)] string? Name,
    [MaxLength(500)] string? TitleTemplate,
    [MaxLength(4000)] string? BodyTemplate,
    bool? IsActive
);