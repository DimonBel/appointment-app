using IdentityApp.Domain.Entity;

namespace IdentityApp.Repository.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetByIdAsync(Guid id);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task<bool> AddAsync(RefreshToken refreshToken);
    Task<bool> UpdateAsync(RefreshToken refreshToken);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> RevokeAllUserTokensAsync(Guid userId);
}
