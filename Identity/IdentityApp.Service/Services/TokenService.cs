using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityApp.Domain.Entity;
using IdentityApp.Domain.Interfaces;
using IdentityApp.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IdentityApp.Service.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public TokenService(IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public string GenerateAccessToken(AppIdentityUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles to claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add custom claims
        if (!string.IsNullOrEmpty(user.FirstName))
            claims.Add(new Claim("FirstName", user.FirstName));
        if (!string.IsNullOrEmpty(user.LastName))
            claims.Add(new Claim("LastName", user.LastName));
        if (!string.IsNullOrEmpty(user.AvatarUrl))
            claims.Add(new Claim("AvatarUrl", user.AvatarUrl));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "30")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _unitOfWork.RefreshTokens.GetByTokenAsync(token);
    }

    public async Task<bool> SaveRefreshTokenAsync(RefreshToken refreshToken)
    {
        var result = await _unitOfWork.RefreshTokens.AddAsync(refreshToken);
        if (result)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return result;
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(token);
        if (refreshToken == null) return false;

        refreshToken.IsRevoked = true;
        await _unitOfWork.RefreshTokens.UpdateAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
    {
        var result = await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userId);
        if (result)
        {
            await _unitOfWork.SaveChangesAsync();
        }
        return result;
    }

    public string? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!");

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = false, // We don't validate expiration here
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
            var jwtId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

            return jwtId;
        }
        catch
        {
            return null;
        }
    }
}
