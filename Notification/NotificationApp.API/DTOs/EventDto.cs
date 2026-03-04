using System.ComponentModel.DataAnnotations;

namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO for submitting a notification event from external services
/// </summary>
public record EventDto(
    [Required] string SourceService,
    [Required] string EventName,
    [Required] string Payload
);