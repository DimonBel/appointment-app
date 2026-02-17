namespace NotificationApp.Repository.Interfaces;

public interface IUnitOfWork : IDisposable
{
    INotificationRepository Notifications { get; }
    INotificationPreferenceRepository Preferences { get; }
    INotificationTemplateRepository Templates { get; }
    INotificationScheduleRepository Schedules { get; }
    INotificationEventRepository Events { get; }
    Task<int> SaveChangesAsync();
}
