using Identity.Domain.Entity;
using Identity.Repository.Interfaces;
using Identity.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Postgres.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<AppIdentityUser?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<AppIdentityUser?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<AppIdentityUser>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task UpdateAsync(AppIdentityUser user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}
