using ChatApp.Domain.Entity;

namespace ChatApp.Repository.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}