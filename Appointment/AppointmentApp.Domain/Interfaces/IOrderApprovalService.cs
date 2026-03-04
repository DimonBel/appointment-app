using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

/// <summary>
/// Service interface for managing order approval workflow and status transitions
/// Handles the state machine for orders: Requested -> Approved -> Completed
/// Also supports Declined and No-Show statuses
/// </summary>
public interface IOrderApprovalService
{
    /// <summary>
    /// Approves an order, transitioning it from Requested to Approved status
    /// Creates order history entry for audit trail
    /// </summary>
    /// <param name="orderId">ID of the order to approve</param>
    /// <param name="reason">Optional reason for approval</param>
    /// <param name="approvedByUserId">ID of the user approving the order (usually the professional)</param>
    /// <returns>Approved order</returns>
    Task<Order> ApproveOrderAsync(Guid orderId, string? reason = null, Guid? approvedByUserId = null);

    /// <summary>
    /// Declines an order, transitioning it to Declined status
    /// Creates order history entry for audit trail
    /// </summary>
    /// <param name="orderId">ID of the order to decline</param>
    /// <param name="reason">Required reason for decline</param>
    /// <param name="declinedByUserId">ID of the user declining the order (usually the professional)</param>
    /// <returns>Declined order</returns>
    Task<Order> DeclineOrderAsync(Guid orderId, string reason, Guid? declinedByUserId = null);

    /// <summary>
    /// Marks an order as completed, transitioning it from Approved to Completed status
    /// Creates order history entry for audit trail
    /// </summary>
    /// <param name="orderId">ID of the order to complete</param>
    /// <param name="notes">Optional notes about the completed appointment</param>
    /// <param name="completedByUserId">ID of the user marking as completed (usually the professional)</param>
    /// <returns>Completed order</returns>
    Task<Order> CompleteOrderAsync(Guid orderId, string? notes = null, Guid? completedByUserId = null);

    /// <summary>
    /// Marks an order as No-Show, transitioning it to NoShow status
    /// Used when client failed to attend the scheduled appointment
    /// </summary>
    /// <param name="orderId">ID of the order to mark as no-show</param>
    /// <param name="notes">Optional notes about the no-show incident</param>
    /// <param name="markedByUserId">ID of the user marking as no-show</param>
    /// <returns>Order marked as no-show</returns>
    Task<Order> MarkAsNoShowAsync(Guid orderId, string? notes = null, Guid? markedByUserId = null);

    /// <summary>
    /// Retrieves the complete history of status changes for an order
    /// </summary>
    /// <param name="orderId">ID of the order</param>
    /// <returns>Collection of order history entries ordered by timestamp</returns>
    Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(Guid orderId);
}