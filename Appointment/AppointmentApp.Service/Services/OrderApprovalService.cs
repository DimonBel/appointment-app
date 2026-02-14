using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service.Services;

public class OrderApprovalService : IOrderApprovalService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderHistoryRepository _orderHistoryRepository;

    public OrderApprovalService(
        IOrderRepository orderRepository,
        IOrderHistoryRepository orderHistoryRepository)
    {
        _orderRepository = orderRepository;
        _orderHistoryRepository = orderHistoryRepository;
    }

    private async Task RecordOrderHistoryAsync(Guid orderId, OrderStatus previousStatus, OrderStatus newStatus, string? reason, Guid? changedByUserId, string? notes)
    {
        var history = new OrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = reason,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        };

        await _orderHistoryRepository.CreateAsync(history);
    }

    public async Task<Order> ApproveOrderAsync(Guid orderId, string? reason = null, Guid? approvedByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Requested)
        {
            throw new InvalidOperationException($"Cannot approve order with status {order.Status}");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Approved;
        order.ApprovalReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        await RecordOrderHistoryAsync(orderId, previousStatus, OrderStatus.Approved, reason, approvedByUserId, null);

        return updatedOrder;
    }

    public async Task<Order> DeclineOrderAsync(Guid orderId, string reason, Guid? declinedByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Requested)
        {
            throw new InvalidOperationException($"Cannot decline order with status {order.Status}");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Declined;
        order.DeclineReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        await RecordOrderHistoryAsync(orderId, previousStatus, OrderStatus.Declined, reason, declinedByUserId, null);

        return updatedOrder;
    }

    public async Task<Order> CompleteOrderAsync(Guid orderId, string? notes = null, Guid? completedByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot complete order with status {order.Status}");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        order.Notes = notes;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        await RecordOrderHistoryAsync(orderId, previousStatus, OrderStatus.Completed, null, completedByUserId, notes);

        return updatedOrder;
    }

    public async Task<Order> MarkAsNoShowAsync(Guid orderId, string? notes = null, Guid? markedByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot mark order as no-show with status {order.Status}");
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.NoShow;
        order.Notes = notes;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        await RecordOrderHistoryAsync(orderId, previousStatus, OrderStatus.NoShow, "No-show", markedByUserId, notes);

        return updatedOrder;
    }

    public async Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(Guid orderId)
    {
        return await _orderHistoryRepository.GetByOrderIdAsync(orderId);
    }
}