using IdentityApp.Domain.Entity;
using IdentityApp.Postgres.Data;
using IdentityApp.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IdentityApp.Postgres.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;

    public RefreshTokenRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Id == id);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(RefreshToken refreshToken)
    {
        try
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAsync(RefreshToken refreshToken)
    {
        try
        {
            _context.RefreshTokens.Update(refreshToken);
            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var token = await GetByIdAsync(id);
            if (token == null) return false;

            _context.RefreshTokens.Remove(token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            var tokens = await GetByUserIdAsync(userId);
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _context.RefreshTokens.Update(token);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
