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

    public async Task<IEnumerable<Order>> GetAllAsync(OrderStatus? status = null, int page = 1, int pageSize = 100, string? sortBy = null, bool descending = false, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Professional)
            .Include(o => o.DomainConfiguration)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        // Add date range filtering for better performance
        // Normalize dates to UTC to avoid PostgreSQL "Kind=Unspecified" error
        if (startDate.HasValue)
        {
            var normalizedStart = startDate.Value.Kind == DateTimeKind.Utc
                ? startDate.Value
                : startDate.Value.Kind == DateTimeKind.Local
                    ? startDate.Value.ToUniversalTime()
                    : DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            query = query.Where(o => o.ScheduledDateTime >= normalizedStart);
        }

        if (endDate.HasValue)
        {
            var normalizedEnd = endDate.Value.Kind == DateTimeKind.Utc
                ? endDate.Value
                : endDate.Value.Kind == DateTimeKind.Local
                    ? endDate.Value.ToUniversalTime()
                    : DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            query = query.Where(o => o.ScheduledDateTime < normalizedEnd.AddDays(1)); // Include the end date
        }

        query = sortBy?.ToLower() switch
        {
            "scheduled" => descending ? query.OrderByDescending(o => o.ScheduledDateTime) : query.OrderBy(o => o.ScheduledDateTime),
            "client" => descending ? query.OrderByDescending(o => o.Client.FirstName) : query.OrderBy(o => o.Client.FirstName),
            "doctor" => descending ? query.OrderByDescending(o => o.Professional.FirstName) : query.OrderBy(o => o.Professional.FirstName),
            "status" => descending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            _ => query.OrderByDescending(o => o.ScheduledDateTime)
        };

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
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

    public async Task<IEnumerable<AppIdentityUser>> GetClientsByProfessionalAsync(Guid professionalId)
    {
        var clientIds = await _context.Orders
            .Where(o => o.ProfessionalId == professionalId)
            .Select(o => o.ClientId)
            .Distinct()
            .ToListAsync();

        return await _context.Users
            .Where(u => clientIds.Contains(u.Id))
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetProfessionalStatisticsAsync(Guid professionalId)
    {
        var statistics = new Dictionary<string, int>();

        // Total clients
        var totalClients = await _context.Orders
            .Where(o => o.ProfessionalId == professionalId)
            .Select(o => o.ClientId)
            .Distinct()
            .CountAsync();
        statistics["totalClients"] = totalClients;

        // Total appointments
        var totalAppointments = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId);
        statistics["totalAppointments"] = totalAppointments;

        // Appointments by status
        var pendingAppointments = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId && o.Status == OrderStatus.Requested);
        statistics["pendingAppointments"] = pendingAppointments;

        var approvedAppointments = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId && o.Status == OrderStatus.Approved);
        statistics["approvedAppointments"] = approvedAppointments;

        var completedAppointments = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId && o.Status == OrderStatus.Completed);
        statistics["completedAppointments"] = completedAppointments;

        var cancelledAppointments = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId && o.Status == OrderStatus.Cancelled);
        statistics["cancelledAppointments"] = cancelledAppointments;

        // Appointments this month
        var now = DateTime.UtcNow;
        var startOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);
        var appointmentsThisMonth = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId && o.ScheduledDateTime >= startOfMonth);
        statistics["appointmentsThisMonth"] = appointmentsThisMonth;

        // Appointments this week
        var startOfWeek = DateTime.SpecifyKind(now.AddDays(-(int)now.DayOfWeek), DateTimeKind.Utc);
        var appointmentsThisWeek = await _context.Orders
            .CountAsync(o => o.ProfessionalId == professionalId && o.ScheduledDateTime >= startOfWeek);
        statistics["appointmentsThisWeek"] = appointmentsThisWeek;

        return statistics;
    }
}
