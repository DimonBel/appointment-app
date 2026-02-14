using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

public interface IOrderApprovalService
{
    Task<Order> ApproveOrderAsync(Guid orderId, string? reason = null, Guid? approvedByUserId = null);
    Task<Order> DeclineOrderAsync(Guid orderId, string reason, Guid? declinedByUserId = null);
    Task<Order> CompleteOrderAsync(Guid orderId, string? notes = null, Guid? completedByUserId = null);
    Task<Order> MarkAsNoShowAsync(Guid orderId, string? notes = null, Guid? markedByUserId = null);
    Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(Guid orderId);
}