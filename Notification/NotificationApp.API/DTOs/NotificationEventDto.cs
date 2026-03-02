namespace NotificationApp.API.DTOs;

/// <summary>
/// DTO representing a notification event
/// </summary>
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