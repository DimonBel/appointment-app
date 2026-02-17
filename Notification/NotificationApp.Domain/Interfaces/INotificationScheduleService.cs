using NotificationApp.Domain.Entity;

namespace NotificationApp.Domain.Interfaces;

/// <summary>
/// Service for managing notification schedules (Module 2.3)
/// </summary>
public interface INotificationScheduleService
{
    Task<NotificationSchedule> CreateAsync(NotificationSchedule schedule);
    Task<NotificationSchedule?> GetByIdAsync(Guid id);
    Task<IEnumerable<NotificationSchedule>> GetPendingSchedulesAsync();
    Task<IEnumerable<NotificationSchedule>> GetByUserIdAsync(Guid userId);
    Task CancelAsync(Guid id);
    Task MarkAsProcessedAsync(Guid id);

    /// <summary>
    /// Schedule a reminder notification before an appointment
    /// </summary>
    Task ScheduleAppointmentReminderAsync(Guid userId, Guid orderId, DateTime appointmentTime, int minutesBefore = 60);

    /// <summary>
    /// Process all pending scheduled notifications
    /// </summary>
    Task ProcessPendingSchedulesAsync();
}
