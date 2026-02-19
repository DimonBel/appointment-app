using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Repository.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<IEnumerable<Order>> GetAllAsync(OrderStatus? status = null, int page = 1, int pageSize = 100, string? sortBy = null, bool descending = false);
    Task<Order> CreateAsync(Order order);
    Task<Order> UpdateAsync(Order order);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Order>> GetByClientAsync(Guid clientId, OrderStatus? status = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<Order>> GetByProfessionalAsync(Guid professionalId, OrderStatus? status = null, int page = 1, int pageSize = 20);
    Task<bool> ExistsAsync(Guid id);
}