using AppointmentApp.Domain.Entity;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class OrderHistoryRepository : IOrderHistoryRepository
{
    private readonly AppointmentDbContext _context;

    public OrderHistoryRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<OrderHistory?> GetByIdAsync(Guid id)
    {
        return await _context.OrderHistory
            .Include(h => h.Order)
            .Include(h => h.ChangedByUser)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<OrderHistory> CreateAsync(OrderHistory orderHistory)
    {
        _context.OrderHistory.Add(orderHistory);
        await _context.SaveChangesAsync();
        return orderHistory;
    }

    public async Task<IEnumerable<OrderHistory>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.OrderHistory
            .Include(h => h.ChangedByUser)
            .Where(h => h.OrderId == orderId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var history = await _context.OrderHistory.FindAsync(id);
        if (history == null) return false;
        _context.OrderHistory.Remove(history);
        await _context.SaveChangesAsync();
        return true;
    }
}