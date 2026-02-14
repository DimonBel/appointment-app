using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(Guid clientId, Guid professionalId, DateTime scheduledDateTime, int durationMinutes, string? title = null, string? description = null, Guid? domainConfigurationId = null);
    Task<Order?> GetOrderByIdAsync(Guid orderId);
    Task<IEnumerable<Order>> GetOrdersByClientAsync(Guid clientId, OrderStatus? status = null, int page = 1, int pageSize = 20);
    Task<IEnumerable<Order>> GetOrdersByProfessionalAsync(Guid professionalId, OrderStatus? status = null, int page = 1, int pageSize = 20);
    Task<Order> UpdateOrderAsync(Guid orderId, string? title = null, string? description = null, string? notes = null);
    Task<Order> CancelOrderAsync(Guid orderId, string? reason = null, Guid? cancelledByUserId = null);
    Task<bool> DeleteOrderAsync(Guid orderId);
}