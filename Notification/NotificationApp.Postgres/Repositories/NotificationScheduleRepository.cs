using Microsoft.EntityFrameworkCore;
using NotificationApp.Domain.Entity;
using NotificationApp.Postgres.Data;
using NotificationApp.Repository.Interfaces;

namespace NotificationApp.Postgres.Repositories;

public class NotificationScheduleRepository : INotificationScheduleRepository
{
    private readonly NotificationDbContext _context;

    public NotificationScheduleRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationSchedule> CreateAsync(NotificationSchedule schedule)
    {
        _context.NotificationSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task<NotificationSchedule?> GetByIdAsync(Guid id)
    {
        return await _context.NotificationSchedules.FindAsync(id);
    }

    public async Task<IEnumerable<NotificationSchedule>> GetPendingAsync(DateTime beforeTime)
    {
        return await _context.NotificationSchedules
            .Where(s => !s.IsProcessed && !s.IsCancelled && s.ScheduledAt <= beforeTime)
            .OrderBy(s => s.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<NotificationSchedule>> GetByUserIdAsync(Guid userId)
    {
        return await _context.NotificationSchedules
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();
    }

    public async Task<NotificationSchedule> UpdateAsync(NotificationSchedule schedule)
    {
        _context.NotificationSchedules.Update(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    public async Task DeleteAsync(Guid id)
    {
        var schedule = await _context.NotificationSchedules.FindAsync(id);
        if (schedule != null)
        {
            _context.NotificationSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }
}
