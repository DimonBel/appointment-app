using AppointmentApp.Domain.Entity;

namespace AppointmentApp.Repository.Interfaces;

public interface IOrderHistoryRepository
{
    Task<OrderHistory?> GetByIdAsync(Guid id);
    Task<OrderHistory> CreateAsync(OrderHistory orderHistory);
    Task<IEnumerable<OrderHistory>> GetByOrderIdAsync(Guid orderId);
    Task<bool> DeleteAsync(Guid id);
}