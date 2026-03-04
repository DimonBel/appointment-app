using NotificationApp.Domain.Entity;
using NotificationApp.Domain.Enums;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service interface for notification delivery and management
/// Handles creating, retrieving, and managing user notifications
/// Supports in-app, email, and real-time delivery channels
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a new notification
    /// </summary>
    /// <param name="notification">Notification to create</param>
    /// <returns>Created notification</returns>
    Task<Notification> CreateAsync(Notification notification);

    /// <summary>
    /// Retrieves a notification by its ID
    /// </summary>
    /// <param name="id">ID of the notification</param>
    /// <returns>Notification if found, null otherwise</returns>
    Task<Notification?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all notifications for a user with pagination
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Collection of user's notifications</returns>
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Retrieves the count of unread notifications for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Number of unread notifications</returns>
    Task<int> GetUnreadCountAsync(Guid userId);

    /// <summary>
    /// Retrieves unread notifications for a user with pagination
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Collection of unread notifications</returns>
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    /// <param name="id">ID of the notification</param>
    Task MarkAsReadAsync(Guid id);

    /// <summary>
    /// Marks all notifications for a user as read
    /// </summary>
    /// <param name="userId">ID of the user</param>
    Task MarkAllAsReadAsync(Guid userId);

    /// <summary>
    /// Deletes a notification
    /// </summary>
    /// <param name="id">ID of the notification to delete</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Retrieves notifications filtered by type for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="type">Notification type to filter by</param>
    /// <returns>Collection of matching notifications</returns>
    Task<IEnumerable<Notification>> GetByTypeAsync(Guid userId, NotificationType type);

    /// <summary>
    /// Sends a notification respecting user preferences
    /// Automatically determines delivery channels based on user preferences
    /// </summary>
    /// <param name="userId">ID of the user to notify</param>
    /// <param name="type">Notification type</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Notification message body</param>
    /// <param name="referenceId">Optional ID of referenced entity</param>
    /// <param name="referenceType">Optional type of referenced entity</param>
    /// <param name="metadata">Optional additional metadata</param>
    Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message,
        Guid? referenceId = null, string? referenceType = null, string? metadata = null);
}