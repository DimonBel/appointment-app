using NotificationApp.Domain.Entity;

namespace NotificationApp.Repository.Interfaces;

public interface INotificationScheduleRepository
{
    Task<NotificationSchedule> CreateAsync(NotificationSchedule schedule);
    Task<NotificationSchedule?> GetByIdAsync(Guid id);
    Task<IEnumerable<NotificationSchedule>> GetPendingAsync(DateTime beforeTime);
    Task<IEnumerable<NotificationSchedule>> GetByUserIdAsync(Guid userId);
    Task<NotificationSchedule> UpdateAsync(NotificationSchedule schedule);
    Task DeleteAsync(Guid id);
}
