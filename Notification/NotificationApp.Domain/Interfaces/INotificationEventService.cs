using NotificationApp.Domain.Entity;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service interface for handling events from other microservices
/// Processes inter-service communication events that trigger notifications
/// Part of event-driven architecture
/// </summary>
public interface INotificationEventService
{
    /// <summary>
    /// Records an incoming event from another microservice
    /// Stores event for processing
    /// </summary>
    /// <param name="sourceService">Name of the source microservice</param>
    /// <param name="eventName">Type/name of the event</param>
    /// <param name="payload">Event data payload</param>
    /// <returns>Recorded notification event</returns>
    Task<NotificationEvent> RecordEventAsync(string sourceService, string eventName, string payload);

    /// <summary>
    /// Processes a recorded event
    /// Triggers appropriate notifications based on event type
    /// </summary>
    /// <param name="eventId">ID of the event to process</param>
    Task ProcessEventAsync(Guid eventId);

    /// <summary>
    /// Retrieves all unprocessed events
    /// Events that have been recorded but not yet processed
    /// </summary>
    /// <returns>Collection of unprocessed events</returns>
    Task<IEnumerable<NotificationEvent>> GetUnprocessedEventsAsync();

    /// <summary>
    /// Retrieves all failed events
    /// Events that failed during processing
    /// </summary>
    /// <returns>Collection of failed events</returns>
    Task<IEnumerable<NotificationEvent>> GetFailedEventsAsync();

    /// <summary>
    /// Retries processing a failed event
    /// </summary>
    /// <param name="eventId">ID of the failed event</param>
    Task RetryFailedEventAsync(Guid eventId);
}