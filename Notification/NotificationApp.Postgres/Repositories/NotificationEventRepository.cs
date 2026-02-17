using Microsoft.EntityFrameworkCore;
using NotificationApp.Domain.Entity;
using NotificationApp.Postgres.Data;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Postgres.Repositories;

public class NotificationEventRepository : INotificationEventRepository
{
    private readonly NotificationDbContext _context;

    public NotificationEventRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationEvent> CreateAsync(NotificationEvent notificationEvent)
    {
        _context.NotificationEvents.Add(notificationEvent);
        await _context.SaveChangesAsync();
        return notificationEvent;
    }

    public async Task<NotificationEvent?> GetByIdAsync(Guid id)
    {
        return await _context.NotificationEvents.FindAsync(id);
    }

    public async Task<IEnumerable<NotificationEvent>> GetUnprocessedAsync()
    {
        return await _context.NotificationEvents
            .Where(e => !e.IsProcessed && e.RetryCount < 3)
            .OrderBy(e => e.ReceivedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationEvent>> GetFailedAsync(int maxRetries = 3)
    {
        return await _context.NotificationEvents
            .Where(e => !e.IsProcessed && e.RetryCount >= maxRetries)
            .OrderByDescending(e => e.ReceivedAt)
            .ToListAsync();
    }

    public async Task<NotificationEvent> UpdateAsync(NotificationEvent notificationEvent)
    {
        _context.NotificationEvents.Update(notificationEvent);
        await _context.SaveChangesAsync();
        return notificationEvent;
    }
}
