using NotificationApp.Domain.Entity;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing scheduled notifications
/// Handles delayed and recurring notifications (reminders, follow-ups, etc.)
/// </summary>
public interface INotificationScheduleService
{
    /// <summary>
    /// Creates a new notification schedule
    /// </summary>
    /// <param name="schedule">Notification schedule to create</param>
    /// <returns>Created notification schedule</returns>
    Task<NotificationSchedule> CreateAsync(NotificationSchedule schedule);

    /// <summary>
    /// Retrieves a notification schedule by its ID
    /// </summary>
    /// <param name="id">ID of the schedule</param>
    /// <returns>Notification schedule if found, null otherwise</returns>
    Task<NotificationSchedule?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all pending (not yet processed) scheduled notifications
    /// </summary>
    /// <returns>Collection of pending schedules</returns>
    Task<IEnumerable<NotificationSchedule>> GetPendingSchedulesAsync();

    /// <summary>
    /// Retrieves all scheduled notifications for a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>Collection of user's scheduled notifications</returns>
    Task<IEnumerable<NotificationSchedule>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Cancels a scheduled notification
    /// </summary>
    /// <param name="id">ID of the schedule to cancel</param>
    Task CancelAsync(Guid id);

    /// <summary>
    /// Marks a scheduled notification as processed
    /// </summary>
    /// <param name="id">ID of the schedule</param>
    Task MarkAsProcessedAsync(Guid id);

    /// <summary>
    /// Schedules a reminder notification before an appointment
    /// Automatically calculates the reminder time based on appointment time
    /// </summary>
    /// <param name="userId">ID of the user to remind</param>
    /// <param name="orderId">ID of the associated order</param>
    /// <param name="appointmentTime">Time of the appointment</param>
    /// <param name="minutesBefore">How many minutes before to send the reminder (default: 60)</param>
    Task ScheduleAppointmentReminderAsync(Guid userId, Guid orderId, DateTime appointmentTime, int minutesBefore = 60);

    /// <summary>
    /// Processes all pending scheduled notifications
    /// Should be called periodically by a background job
    /// </summary>
    Task ProcessPendingSchedulesAsync();
}