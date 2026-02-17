using AppointmentApp.Domain.Entity;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class ProfessionalRepository : IProfessionalRepository
{
    private readonly AppointmentDbContext _context;

    public ProfessionalRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<Professional?> GetByIdAsync(Guid id)
    {
        return await _context.Professionals
            .Include(p => p.User)
            .Include(p => p.Availabilities)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Professional?> GetByUserIdAsync(Guid userId)
    {
        return await _context.Professionals
            .Include(p => p.User)
            .Include(p => p.Availabilities)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<Professional> CreateAsync(Professional professional)
    {
        _context.Professionals.Add(professional);
        await _context.SaveChangesAsync();
        return professional;
    }

    public async Task<Professional> UpdateAsync(Professional professional)
    {
        professional.UpdatedAt = DateTime.UtcNow;
        _context.Professionals.Update(professional);
        await _context.SaveChangesAsync();
        return professional;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var professional = await _context.Professionals.FindAsync(id);
        if (professional == null) return false;
        _context.Professionals.Remove(professional);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Professional>> GetAllAsync(bool onlyAvailable = true, int page = 1, int pageSize = 20)
    {
        IQueryable<Professional> query = _context.Professionals
            .Include(p => p.User);

        if (onlyAvailable)
        {
            query = query.Where(p => p.IsAvailable);
        }

        return await query
            .OrderBy(p => p.User != null ? p.User.LastName : "")
            .ThenBy(p => p.User != null ? p.User.FirstName : "")
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Professionals.AnyAsync(p => p.Id == id);
    }
}