using NotificationApp.Postgres.Data;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Postgres.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationDbContext _context;

    public INotificationRepository Notifications { get; }
    public INotificationPreferenceRepository Preferences { get; }
    public INotificationTemplateRepository Templates { get; }
    public INotificationScheduleRepository Schedules { get; }
    public INotificationEventRepository Events { get; }

    public UnitOfWork(
        NotificationDbContext context,
        INotificationRepository notifications,
        INotificationPreferenceRepository preferences,
        INotificationTemplateRepository templates,
        INotificationScheduleRepository schedules,
        INotificationEventRepository events)
    {
        _context = context;
        Notifications = notifications;
        Preferences = preferences;
        Templates = templates;
        Schedules = schedules;
        Events = events;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
