using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppointmentDbContext _context;

    public OrderRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Professional)
            .Include(o => o.DomainConfiguration)
            .Include(o => o.PreOrderData)
            .Include(o => o.OrderHistory)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return false;
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Order>> GetByClientAsync(Guid clientId, OrderStatus? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .Include(o => o.Professional)
            .Include(o => o.DomainConfiguration)
            .Where(o => o.ClientId == clientId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetByProfessionalAsync(Guid professionalId, OrderStatus? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Orders
            .Include(o => o.Client)
            .Include(o => o.DomainConfiguration)
            .Where(o => o.ProfessionalId == professionalId);

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        return await query
            .OrderBy(o => o.ScheduledDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }
}