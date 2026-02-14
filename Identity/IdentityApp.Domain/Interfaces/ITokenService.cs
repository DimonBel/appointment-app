using IdentityApp.Domain.Entity;

namespace IdentityApp.Domain.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AppIdentityUser user, IList<string> roles);
    string GenerateRefreshToken();
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken);
    Task<bool> RevokeRefreshTokenAsync(string token);
    Task<bool> RevokeAllUserTokensAsync(Guid userId);
    string? ValidateToken(string token);
}
