using ChatApp.Domain.Entity;
using ChatApp.Repository.Interfaces;
using ChatApp.Postgres.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatApp.Postgres.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<AppIdentityUser> _userManager;
    private readonly IMemoryCache _cache;
    private const int CacheExpirationMinutes = 5;

    public UserRepository(UserManager<AppIdentityUser> userManager, IMemoryCache cache)
    {
        _userManager = userManager;
        _cache = cache;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"user_{id}";
        
        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out User? cachedUser))
        {
            return cachedUser;
        }
        
        // If not in cache, get from database
        var identityUser = await _userManager.FindByIdAsync(id.ToString());
        if (identityUser == null)
        {
            return null;
        }
        
        var user = MapToUser(identityUser);
        
        // Store in cache for 5 minutes
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
        _cache.Set(cacheKey, user, cacheOptions);
        
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var identityUser = await _userManager.FindByEmailAsync(email);
        return identityUser != null ? MapToUser(identityUser) : null;
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        var identityUser = await _userManager.FindByNameAsync(userName);
        return identityUser != null ? MapToUser(identityUser) : null;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var identityUsers = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
        return identityUsers.Select(MapToUser);
    }

    public async Task<User> CreateAsync(User user)
    {
        var identityUser = new AppIdentityUser
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            CreatedAt = user.CreatedAt,
            IsOnline = user.IsOnline
        };

        var result = await _userManager.CreateAsync(identityUser);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return MapToUser(identityUser);
    }

    public async Task UpdateAsync(User user)
    {
        var identityUser = await _userManager.FindByIdAsync(user.Id.ToString());
        if (identityUser == null)
        {
            throw new Exception("User not found");
        }

        identityUser.UserName = user.UserName;
        identityUser.Email = user.Email;
        identityUser.AvatarUrl = user.AvatarUrl ?? string.Empty;
        identityUser.IsOnline = user.IsOnline;

        var result = await _userManager.UpdateAsync(identityUser);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        
        // Invalidate cache after update
        var cacheKey = $"user_{user.Id}";
        _cache.Remove(cacheKey);
    }

    public async Task DeleteAsync(Guid id)
    {
        var identityUser = await _userManager.FindByIdAsync(id.ToString());
        if (identityUser != null)
        {
            var result = await _userManager.DeleteAsync(identityUser);
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to delete user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
            
            // Invalidate cache after delete
            var cacheKey = $"user_{id}";
            _cache.Remove(cacheKey);
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _userManager.Users.AnyAsync(u => u.Id == id);
    }

    private static User MapToUser(AppIdentityUser identityUser)
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