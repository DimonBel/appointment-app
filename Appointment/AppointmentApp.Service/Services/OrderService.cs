using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IProfessionalRepository professionalRepository,
        IAvailabilitySlotRepository availabilitySlotRepository)
    {
        _orderRepository = orderRepository;
        _professionalRepository = professionalRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
    }

    public async Task<Order> CreateOrderAsync(Guid clientId, Guid professionalId, DateTime scheduledDateTime, int durationMinutes, string? title = null, string? description = null, Guid? domainConfigurationId = null)
    {
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional == null)
        {
            throw new ArgumentException("Professional not found", nameof(professionalId));
        }

        if (!professional.IsAvailable)
        {
            throw new InvalidOperationException("Professional is not available for booking");
        }

        var isAvailable = await _availabilitySlotRepository.IsSlotAvailableAsync(professionalId, scheduledDateTime, durationMinutes);
        if (!isAvailable)
        {
            throw new InvalidOperationException("Requested time slot is not available");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professionalId,
            ScheduledDateTime = scheduledDateTime,
            DurationMinutes = durationMinutes,
            Title = title,
            Description = description,
            DomainConfigurationId = domainConfigurationId,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        return await _orderRepository.CreateAsync(order);
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        return await _orderRepository.GetByIdAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetOrdersByClientAsync(Guid clientId, OrderStatus? status = null, int page = 1, int pageSize = 20)
    {
        return await _orderRepository.GetByClientAsync(clientId, status, page, pageSize);
    }

    public async Task<IEnumerable<Order>> GetOrdersByProfessionalAsync(Guid professionalId, OrderStatus? status = null, int page = 1, int pageSize = 20)
    {
        return await _orderRepository.GetByProfessionalAsync(professionalId, status, page, pageSize);
    }

    public async Task<Order> UpdateOrderAsync(Guid orderId, string? title = null, string? description = null, string? notes = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (title != null) order.Title = title;
        if (description != null) order.Description = description;
        if (notes != null) order.Notes = notes;

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<Order> CancelOrderAsync(Guid orderId, string? reason = null, Guid? cancelledByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Requested && order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot cancel order with status {order.Status}");
        }

        order.Status = OrderStatus.Cancelled;
        order.Notes = reason;
        order.UpdatedAt = DateTime.UtcNow;

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        return await _orderRepository.DeleteAsync(orderId);
    }
}