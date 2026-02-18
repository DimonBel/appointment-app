using Identity.Domain.Entity;

namespace Identity.Repository.Interfaces;

public interface IUserRepository
{
    Task<AppIdentityUser?> GetByIdAsync(Guid id);
    Task<AppIdentityUser?> GetByEmailAsync(string email);
    Task<IEnumerable<AppIdentityUser>> GetAllAsync();
    Task UpdateAsync(AppIdentityUser user);
}
