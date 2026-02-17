using IdentityApp.Domain.Entity;
using IdentityApp.Postgres.Data;
using IdentityApp.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IdentityApp.Postgres.Repositories;

public class DoctorProfileRepository : IDoctorProfileRepository
{
    private readonly IdentityDbContext _context;

    public DoctorProfileRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorProfile?> GetByIdAsync(Guid id)
    {
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.Id == id);
    }

    public async Task<DoctorProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.UserId == userId);
    }

    public async Task<IEnumerable<DoctorProfile>> GetAllAsync()
    {
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.IsAvailableForAppointments)
            .ToListAsync();
    }

    public async Task<IEnumerable<DoctorProfile>> GetBySpecialtyAsync(string specialty)
    {
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.Specialty == specialty && dp.IsAvailableForAppointments)
            .ToListAsync();
    }

    public async Task<IEnumerable<DoctorProfile>> SearchAsync(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.IsAvailableForAppointments &&
                (dp.Specialty != null && dp.Specialty.ToLower().Contains(lowerQuery) ||
                 dp.User.FirstName != null && dp.User.FirstName.ToLower().Contains(lowerQuery) ||
                 dp.User.LastName != null && dp.User.LastName.ToLower().Contains(lowerQuery) ||
                 dp.City != null && dp.City.ToLower().Contains(lowerQuery)))
            .ToListAsync();
    }

    public async Task<DoctorProfile> CreateAsync(DoctorProfile profile)
    {
        profile.CreatedAt = DateTime.UtcNow;
        _context.DoctorProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Reload with User navigation property
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .FirstAsync(dp => dp.Id == profile.Id);
    }

    public async Task<DoctorProfile> UpdateAsync(DoctorProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        _context.DoctorProfiles.Update(profile);
        await _context.SaveChangesAsync();

        // Reload with User navigation property
        return await _context.DoctorProfiles
            .Include(dp => dp.User)
            .FirstAsync(dp => dp.Id == profile.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var profile = await _context.DoctorProfiles.FindAsync(id);
        if (profile != null)
        {
            _context.DoctorProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }
    }
}
