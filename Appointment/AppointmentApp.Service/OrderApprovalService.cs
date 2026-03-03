using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service;

/// <summary>
/// Service for managing order state transitions (approve, decline, complete, cancel)
/// Enforces order state machine rules and maintains complete audit trail via OrderHistory
/// </summary>
public class OrderApprovalService : IOrderApprovalService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderHistoryRepository _orderHistoryRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;

    public OrderApprovalService(
        IOrderRepository orderRepository,
        IOrderHistoryRepository orderHistoryRepository,
        IProfessionalRepository professionalRepository,
        IAvailabilitySlotRepository availabilitySlotRepository)
    {
        _orderRepository = orderRepository;
        _orderHistoryRepository = orderHistoryRepository;
        _professionalRepository = professionalRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
    }

    /// <summary>
    /// Records a state transition in the order history for audit purposes
    /// </summary>
    /// <param name="orderId">ID of the order</param>
    /// <param name="previousStatus">Status before transition</param>
    /// <param name="newStatus">Status after transition</param>
    /// <param name="reason">Optional reason for the transition</param>
    /// <param name="changedByUserId">ID of user who initiated the transition</param>
    /// <param name="notes">Additional notes about the transition</param>
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

    /// <summary>
    /// Approves an order (Requested → Approved)
    /// Records approval in order history with optional reason
    /// </summary>
    /// <param name="orderId">ID of the order to approve</param>
    /// <param name="reason">Optional reason for approval</param>
    /// <param name="approvedByUserId">ID of user approving the order</param>
    /// <returns>Updated order with Approved status</returns>
    /// <exception cref="ArgumentException">Order not found</exception>
    /// <exception cref="InvalidOperationException">Order is not in Requested status</exception>
    /// <inheritdoc/>
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

        var professional = await _professionalRepository.GetByUserIdAsync(order.ProfessionalId);
        if (professional == null)
        {
            throw new InvalidOperationException("Professional profile not found");
        }

        var normalizedScheduledDateTime = NormalizeToUtc(order.ScheduledDateTime);
        var requestedStartTime = normalizedScheduledDateTime.TimeOfDay;
        var requestedEndTime = requestedStartTime.Add(TimeSpan.FromMinutes(order.DurationMinutes));
        var requestedSlots = (await _availabilitySlotRepository.GetSlotsByDateAsync(professional.Id, normalizedScheduledDateTime.Date))
            .Where(s => s.StartTime >= requestedStartTime && s.StartTime < requestedEndTime)
            .OrderBy(s => s.StartTime)
            .ToList();

        if (requestedSlots.Count == 0)
        {
            throw new InvalidOperationException("Requested time slot is not available");
        }

        var hasAvailableSlots = requestedSlots.Any(s => s.IsAvailable);
        var hasReservedSlots = requestedSlots.Any(s => !s.IsAvailable);

        // Mixed state means the interval is partially occupied by another booking.
        if (hasAvailableSlots && hasReservedSlots)
        {
            throw new InvalidOperationException("Requested time slot is not available");
        }

        // Reserve slots only for legacy orders where slots were not reserved during creation.
        if (hasAvailableSlots)
        {
            foreach (var slot in requestedSlots)
            {
                slot.IsAvailable = false;
                await _availabilitySlotRepository.UpdateAsync(slot);
            }
        }

        var previousStatus = order.Status;
        order.Status = OrderStatus.Approved;
        order.ApprovalReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        await RecordOrderHistoryAsync(orderId, previousStatus, OrderStatus.Approved, reason, approvedByUserId, null);

        return updatedOrder;
    }

    private static DateTime NormalizeToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            return dateTime;
        }

        if (dateTime.Kind == DateTimeKind.Local)
        {
            return dateTime.ToUniversalTime();
        }

        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    /// <inheritdoc/>
    public async Task<Order> DeclineOrderAsync(Guid orderId, string reason, Guid? declinedByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Requested && order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot decline order with status {order.Status}");
        }

        var previousStatus = order.Status;

        // Release reserved slots for both Requested and Approved orders
        if (previousStatus == OrderStatus.Requested || previousStatus == OrderStatus.Approved)
        {
            await ReleaseReservedSlotsAsync(order.ProfessionalId, order.ScheduledDateTime, order.DurationMinutes);
        }

        order.Status = OrderStatus.Declined;
        order.DeclineReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);
        await RecordOrderHistoryAsync(orderId, previousStatus, OrderStatus.Declined, reason, declinedByUserId, null);

        return updatedOrder;
    }

    /// <inheritdoc/>
    public async Task<Order> CompleteOrderAsync(Guid orderId, string? notes = null, Guid? completedByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Approved && order.Status != OrderStatus.Requested)
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<IEnumerable<OrderHistory>> GetOrderHistoryAsync(Guid orderId)
    {
        return await _orderHistoryRepository.GetByOrderIdAsync(orderId);
    }

    private async Task ReleaseReservedSlotsAsync(Guid professionalUserId, DateTime scheduledDateTime, int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            return;
        }

        var professional = await _professionalRepository.GetByUserIdAsync(professionalUserId);
        if (professional == null)
        {
            return;
        }

        var startDateTimeUtc = NormalizeToUtc(scheduledDateTime);
        var requestedStartTime = startDateTimeUtc.TimeOfDay;
        var requestedEndTime = requestedStartTime.Add(TimeSpan.FromMinutes(durationMinutes));

        var daySlots = (await _availabilitySlotRepository.GetSlotsByDateAsync(professional.Id, startDateTimeUtc.Date))
            .Where(slot => slot.StartTime >= requestedStartTime && slot.StartTime < requestedEndTime)
            .ToList();

        foreach (var slot in daySlots)
        {
            if (slot.IsAvailable)
            {
                continue;
            }

            slot.IsAvailable = true;
            await _availabilitySlotRepository.UpdateAsync(slot);
        }
    }
}