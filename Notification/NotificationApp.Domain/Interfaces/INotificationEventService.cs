using NotificationApp.Domain.Entity;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service for handling events from other microservices (Module 2.5)
/// </summary>
public interface INotificationEventService
{
    Task<NotificationEvent> RecordEventAsync(string sourceService, string eventName, string payload);
    Task ProcessEventAsync(Guid eventId);
    Task<IEnumerable<NotificationEvent>> GetUnprocessedEventsAsync();
    Task<IEnumerable<NotificationEvent>> GetFailedEventsAsync();
    Task RetryFailedEventAsync(Guid eventId);
}
