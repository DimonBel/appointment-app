using AppointmentApp.Domain.Entity;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class PreOrderDataRepository : IPreOrderDataRepository
{
    private readonly AppointmentDbContext _context;

    public PreOrderDataRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<PreOrderData?> GetByIdAsync(Guid id)
    {
        return await _context.PreOrderData
            .Include(p => p.Order)
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PreOrderData?> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.PreOrderData
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<PreOrderData> CreateAsync(PreOrderData preOrderData)
    {
        _context.PreOrderData.Add(preOrderData);
        await _context.SaveChangesAsync();
        return preOrderData;
    }

    public async Task<PreOrderData> UpdateAsync(PreOrderData preOrderData)
    {
        preOrderData.UpdatedAt = DateTime.UtcNow;
        _context.PreOrderData.Update(preOrderData);
        await _context.SaveChangesAsync();
        return preOrderData;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var data = await _context.PreOrderData.FindAsync(id);
        if (data == null) return false;
        _context.PreOrderData.Remove(data);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.PreOrderData.AnyAsync(p => p.Id == id);
    }
}