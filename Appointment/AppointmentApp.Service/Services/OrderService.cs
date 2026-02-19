using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AppointmentApp.Service.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProfessionalRepository _professionalRepository;
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly UserManager<AppIdentityUser> _userManager;

    public OrderService(
        IOrderRepository orderRepository,
        IProfessionalRepository professionalRepository,
        IAvailabilitySlotRepository availabilitySlotRepository,
        UserManager<AppIdentityUser> userManager)
    {
        _orderRepository = orderRepository;
        _professionalRepository = professionalRepository;
        _availabilitySlotRepository = availabilitySlotRepository;
        _userManager = userManager;
    }

    public async Task<Order> CreateOrderAsync(Guid clientId, Guid professionalId, DateTime scheduledDateTime, int durationMinutes, string? title = null, string? description = null, Guid? domainConfigurationId = null)
    {
        var normalizedScheduledDateTime = NormalizeToUtc(scheduledDateTime);

        // Check if client user exists, if not create a minimal shadow user
        var existingClient = await _userManager.FindByIdAsync(clientId.ToString());
        if (existingClient == null)
        {
            var shadowClient = new AppIdentityUser
            {
                Id = clientId,
                UserName = $"client_{clientId:N}",
                Email = $"client_{clientId:N}@shadow.local",
                FirstName = "Client",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(shadowClient);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create local appointment client: {errors}");
            }
        }

        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional == null)
        {
            throw new ArgumentException("Professional not found", nameof(professionalId));
        }

        if (!professional.IsAvailable)
        {
            throw new InvalidOperationException("Professional is not available for booking");
        }

        var isAvailable = await _availabilitySlotRepository.IsSlotAvailableAsync(professionalId, normalizedScheduledDateTime, durationMinutes);
        if (!isAvailable)
        {
            throw new InvalidOperationException("Requested time slot is not available");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professional.UserId, // Use the professional's UserId, not the Professional entity Id
            ScheduledDateTime = normalizedScheduledDateTime,
            DurationMinutes = durationMinutes,
            Title = title,
            Description = description,
            DomainConfigurationId = domainConfigurationId,
            Status = OrderStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.CreateAsync(order);

        // Reload with navigation properties populated
        return await _orderRepository.GetByIdAsync(order.Id) ?? order;
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

    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        return await _orderRepository.GetByIdAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetAllOrdersAsync(OrderStatus? status = null, int page = 1, int pageSize = 100, string? sortBy = null, bool descending = false)
    {
        return await _orderRepository.GetAllAsync(status, page, pageSize, sortBy, descending);
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

        var previousStatus = order.Status;

        if (order.Status != OrderStatus.Requested && order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot cancel order with status {order.Status}");
        }

        if (previousStatus == OrderStatus.Approved)
        {
            await ReleaseReservedSlotsAsync(order.ProfessionalId, order.ScheduledDateTime, order.DurationMinutes);
        }

        order.Status = OrderStatus.Cancelled;
        order.Notes = reason;
        order.UpdatedAt = DateTime.UtcNow;

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<Order> RescheduleOrderAsync(Guid orderId, DateTime newScheduledDateTime, string? notes = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        if (order.Status != OrderStatus.Requested && order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot reschedule order with status {order.Status}");
        }

        if (newScheduledDateTime <= DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled date and time must be in the future", nameof(newScheduledDateTime));
        }

        order.ScheduledDateTime = NormalizeToUtc(newScheduledDateTime);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            order.Notes = notes;
        }
        order.UpdatedAt = DateTime.UtcNow;

        return await _orderRepository.UpdateAsync(order);
    }

    public async Task<bool> DeleteOrderAsync(Guid orderId)
    {
        return await _orderRepository.DeleteAsync(orderId);
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