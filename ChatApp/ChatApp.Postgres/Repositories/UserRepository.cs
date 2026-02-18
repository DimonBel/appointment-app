using ChatApp.Domain.Entity;
using ChatApp.Repository.Interfaces;
using ChatApp.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Postgres.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var identityUser = await _context.Users.FindAsync(id);
        if (identityUser == null) return null;
        return ConvertToUser(identityUser);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var identityUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (identityUser == null) return null;
        return ConvertToUser(identityUser);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        var identityUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (identityUser == null) return null;
        return ConvertToUser(identityUser);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var identityUsers = await _context.Users.ToListAsync();
        return identityUsers.Select(ConvertToUser);
    }

    public async Task<User> CreateAsync(User user)
    {
        var identityUser = new AppIdentityUser
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            IsOnline = user.IsOnline
        };

        _context.Users.Add(identityUser);
        await _context.SaveChangesAsync();

        return ConvertToUser(identityUser);
    }

    public async Task UpdateAsync(User user)
    {
        var identityUser = await _context.Users.FindAsync(user.Id);
        if (identityUser != null)
        {
            identityUser.UserName = user.UserName;
            identityUser.Email = user.Email;
            identityUser.AvatarUrl = user.AvatarUrl;
            identityUser.IsOnline = user.IsOnline;

            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var identityUser = await _context.Users.FindAsync(id);
        if (identityUser != null)
        {
            _context.Users.Remove(identityUser);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    private static User ConvertToUser(AppIdentityUser identityUser)
    {
        return new User
        {
            Id = identityUser.Id,
            UserName = identityUser.UserName ?? string.Empty,
            Email = identityUser.Email ?? string.Empty,
            AvatarUrl = identityUser.AvatarUrl,
            CreatedAt = identityUser.CreatedAt,
            IsOnline = identityUser.IsOnline
        };
    }
}
