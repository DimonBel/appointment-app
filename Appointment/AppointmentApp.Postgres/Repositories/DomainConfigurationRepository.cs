using AppointmentApp.Domain.Entity;
using AppointmentApp.Repository.Interfaces;
using AppointmentApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentApp.Postgres.Repositories;

public class DomainConfigurationRepository : IDomainConfigurationRepository
{
    private readonly AppointmentDbContext _context;

    public DomainConfigurationRepository(AppointmentDbContext context)
    {
        _context = context;
    }

    public async Task<DomainConfiguration?> GetByIdAsync(Guid id)
    {
        return await _context.DomainConfigurations
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DomainConfiguration> CreateAsync(DomainConfiguration domainConfiguration)
    {
        _context.DomainConfigurations.Add(domainConfiguration);
        await _context.SaveChangesAsync();
        return domainConfiguration;
    }

    public async Task<DomainConfiguration> UpdateAsync(DomainConfiguration domainConfiguration)
    {
        domainConfiguration.UpdatedAt = DateTime.UtcNow;
        _context.DomainConfigurations.Update(domainConfiguration);
        await _context.SaveChangesAsync();
        return domainConfiguration;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var config = await _context.DomainConfigurations.FindAsync(id);
        if (config == null) return false;
        _context.DomainConfigurations.Remove(config);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<DomainConfiguration>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.DomainConfigurations.AsQueryable();

        if (onlyActive)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.DomainConfigurations.AnyAsync(d => d.Id == id);
    }
}