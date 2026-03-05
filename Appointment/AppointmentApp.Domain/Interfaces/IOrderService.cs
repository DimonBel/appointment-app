using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;

namespace AppointmentApp.Domain.Interfaces;

/// <summary>
/// Core service interface for managing appointment orders
/// Handles order creation, retrieval, modification, cancellation, and rescheduling
/// Manages orders across multiple domains (medical, legal, consulting, etc.)
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new appointment order with full validation and availability checking
    /// Automatically creates shadow user if client doesn't exist locally
    /// Validates professional availability and time slot availability before creation
    /// </summary>
    /// <param name="clientId">ID of the client placing the order</param>
    /// <param name="professionalId">ID of the professional to book</param>
    /// <param name="scheduledDateTime">Appointment date and time</param>
    /// <param name="durationMinutes">Duration of the appointment in minutes</param>
    /// <param name="title">Optional title for the order</param>
    /// <param name="description">Optional description for the order</param>
    /// <param name="domainConfigurationId">Optional domain configuration ID</param>
    /// <returns>Created order with status Requested</returns>
    /// <exception cref="ArgumentException">Professional not found</exception>
    /// <exception cref="InvalidOperationException">Professional unavailable or time slot not available</exception>
    Task<Order> CreateOrderAsync(Guid clientId, Guid professionalId, DateTime scheduledDateTime, int durationMinutes, string? title = null, string? description = null, Guid? domainConfigurationId = null);

    /// <summary>
    /// Retrieves an order by its ID
    /// </summary>
    /// <param name="orderId">ID of the order</param>
    /// <returns>Order if found, null otherwise</returns>
    Task<Order?> GetOrderByIdAsync(Guid orderId);

    /// <summary>
    /// Retrieves all orders with optional filtering and pagination
    /// </summary>
    /// <param name="status">Optional filter by order status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="sortBy">Optional field to sort by</param>
    /// <param name="descending">Sort direction (true for descending)</param>
    /// <param name="startDate">Optional start date for filtering orders</param>
    /// <param name="endDate">Optional end date for filtering orders</param>
    /// <returns>Collection of orders</returns>
    Task<IEnumerable<Order>> GetAllOrdersAsync(OrderStatus? status = null, int page = 1, int pageSize = 100, string? sortBy = null, bool descending = false, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Retrieves all orders for a specific client with optional filtering and pagination
    /// </summary>
    /// <param name="clientId">ID of the client</param>
    /// <param name="status">Optional filter by order status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Collection of client's orders</returns>
    Task<IEnumerable<Order>> GetOrdersByClientAsync(Guid clientId, OrderStatus? status = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Retrieves all orders for a specific professional with optional filtering and pagination
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <param name="status">Optional filter by order status</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Collection of professional's orders</returns>
    Task<IEnumerable<Order>> GetOrdersByProfessionalAsync(Guid professionalId, OrderStatus? status = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Retrieves all unique clients who have booked appointments with a professional
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <returns>Collection of client users</returns>
    Task<IEnumerable<AppIdentityUser>> GetClientsByProfessionalAsync(Guid professionalId);

    /// <summary>
    /// Retrieves statistics for a specific professional
    /// </summary>
    /// <param name="professionalId">ID of the professional</param>
    /// <returns>Dictionary containing various statistics</returns>
    Task<Dictionary<string, int>> GetProfessionalStatisticsAsync(Guid professionalId);

    /// <summary>
    /// Updates order details (title, description, notes)
    /// Cannot modify scheduled date/time or participants - use RescheduleOrderAsync for that
    /// </summary>
    /// <param name="orderId">ID of the order to update</param>
    /// <param name="title">Optional new title</param>
    /// <param name="description">Optional new description</param>
    /// <param name="notes">Optional new notes</param>
    /// <returns>Updated order</returns>
    Task<Order> UpdateOrderAsync(Guid orderId, string? title = null, string? description = null, string? notes = null);

    /// <summary>
    /// Cancels an order, transitioning it to Cancelled status
    /// Releases the booked time slot for availability
    /// </summary>
    /// <param name="orderId">ID of the order to cancel</param>
    /// <param name="reason">Optional reason for cancellation</param>
    /// <param name="cancelledByUserId">ID of the user cancelling the order</param>
    /// <returns>Cancelled order</returns>
    Task<Order> CancelOrderAsync(Guid orderId, string? reason = null, Guid? cancelledByUserId = null);

    /// <summary>
    /// Reschedules an order to a new date and time
    /// Validates availability of the new slot before rescheduling
    /// </summary>
    /// <param name="orderId">ID of the order to reschedule</param>
    /// <param name="newScheduledDateTime">New appointment date and time</param>
    /// <param name="notes">Optional notes about the reschedule</param>
    /// <returns>Rescheduled order</returns>
    Task<Order> RescheduleOrderAsync(Guid orderId, DateTime newScheduledDateTime, string? notes = null);

    /// <summary>
    /// Permanently deletes an order (use with caution)
    /// Only allowed for orders in certain statuses (e.g., never used in production)
    /// </summary>
    /// <param name="orderId">ID of the order to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteOrderAsync(Guid orderId);
}