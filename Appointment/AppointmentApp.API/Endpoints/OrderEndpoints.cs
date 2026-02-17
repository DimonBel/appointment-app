using AppointmentApp.API.DTOs;
using AppointmentApp.API.Services;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AppointmentApp.API.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");
        // .RequireAuthorization(); // Temporarily disabled for testing

        // Get all orders for current user
        group.MapGet("/", async (
            [FromServices] IOrderService orderService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var userId = ResolveUserId(context);
            if (!userId.HasValue)
            {
                return Results.Unauthorized();
            }

            var orders = await orderService.GetOrdersByClientAsync(userId.Value, status, page, pageSize);
            return Results.Ok(orders);
        })
        .WithName("GetAllOrders")
        .WithOpenApi();

        // Create order
        group.MapPost("/", async (
            [FromBody] CreateOrderDto dto,
            [FromServices] IOrderService orderService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            [FromServices] IHttpClientFactory httpClientFactory,
            HttpContext context) =>
        {
            var clientId = ResolveUserId(context);
            if (!clientId.HasValue)
            {
                return Results.Unauthorized();
            }

            var order = await orderService.CreateOrderAsync(
                clientId.Value,
                dto.ProfessionalId,
                dto.ScheduledDateTime,
                dto.DurationMinutes,
                dto.Title,
                dto.Description,
                dto.DomainConfigurationId);

            var localUser = await userManager.FindByIdAsync(clientId.Value.ToString());
            var userName = context.User.FindFirstValue(ClaimTypes.Name)
                ?? context.User.FindFirstValue("name")
                ?? localUser?.UserName
                ?? "Patient";
            var userEmail = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue("email")
                ?? localUser?.Email;

            // Fire booking confirmation notification (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("NotificationService");
                    var payload = JsonSerializer.Serialize(new
                    {
                        userId = clientId.Value,
                        userName,
                        email = userEmail,
                        orderId = order.Id,
                        doctorName = "Doctor",
                        appointmentDate = dto.ScheduledDateTime.ToString("yyyy-MM-dd"),
                        appointmentTime = dto.ScheduledDateTime.ToString("HH:mm"),
                        title = dto.Title ?? "Appointment",
                        scheduledDateTime = dto.ScheduledDateTime,
                        professionalId = dto.ProfessionalId
                    });
                    await client.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "AppointmentService",
                        eventName = "BookingConfirmed",
                        payload
                    });
                }
                catch { /* non-critical */ }
            });

            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .WithName("CreateOrder")
        .WithOpenApi();

        // Get order by ID
        group.MapGet("/{id}", async (
            Guid id,
            [FromServices] IOrderService orderService) =>
        {
            var order = await orderService.GetOrderByIdAsync(id);
            return order != null ? Results.Ok(order) : Results.NotFound();
        })
        .WithName("GetOrderById")
        .WithOpenApi();

        // Get orders by client
        group.MapGet("/client/{clientId}", async (
            Guid clientId,
            [FromServices] IOrderService orderService,
            [FromServices] IProfessionalRepository professionalRepo,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var orders = await orderService.GetOrdersByClientAsync(clientId, status, page, pageSize);

            // Enrich orders with professional entity data
            var enrichedOrders = new List<object>();
            foreach (var order in orders)
            {
                Professional? professionalEntity = null;
                if (order.ProfessionalId != Guid.Empty)
                {
                    var allProfessionals = await professionalRepo.GetAllAsync();
                    professionalEntity = allProfessionals.FirstOrDefault(p => p.UserId == order.ProfessionalId);
                }

                enrichedOrders.Add(new
                {
                    order.Id,
                    order.ClientId,
                    order.ProfessionalId,
                    order.DomainConfigurationId,
                    order.DomainType,
                    order.Status,
                    order.ScheduledDateTime,
                    order.DurationMinutes,
                    order.Title,
                    order.Description,
                    order.Notes,
                    order.DeclineReason,
                    order.ApprovalReason,
                    order.CreatedAt,
                    order.UpdatedAt,
                    order.CompletedAt,
                    order.PreOrderDataId,
                    order.Client,
                    Professional = order.Professional,
                    ProfessionalEntity = professionalEntity != null ? new
                    {
                        professionalEntity.Id,
                        professionalEntity.UserId,
                        professionalEntity.Title,
                        professionalEntity.Specialization,
                        professionalEntity.HourlyRate
                    } : null,
                    order.DomainConfiguration,
                    order.PreOrderData,
                    order.OrderHistory
                });
            }

            return Results.Ok(enrichedOrders);
        })
        .WithName("GetOrdersByClient")
        .WithOpenApi();

        // Get orders by professional
        group.MapGet("/professional/{professionalId}", async (
            Guid professionalId,
            [FromServices] IOrderService orderService,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var orders = await orderService.GetOrdersByProfessionalAsync(professionalId, status, page, pageSize);
            return Results.Ok(orders);
        })
        .WithName("GetOrdersByProfessional")
        .WithOpenApi();

        // Update order
        group.MapPut("/{id}", async (
            Guid id,
            [FromBody] UpdateOrderDto dto,
            [FromServices] IOrderService orderService) =>
        {
            var order = await orderService.UpdateOrderAsync(id, dto.Title, dto.Description, dto.Notes);
            return Results.Ok(order);
        })
        .WithName("UpdateOrder")
        .WithOpenApi();

        // Cancel order
        group.MapPost("/{id}/cancel", async (
            Guid id,
            [FromBody] CancelOrderDto? dto,
            [FromServices] IOrderService orderService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            var cancelledByUserId = ResolveUserId(context);
            if (!cancelledByUserId.HasValue)
            {
                return Results.Unauthorized();
            }

            var order = await orderService.CancelOrderAsync(id, dto?.Reason, cancelledByUserId.Value);
            return Results.Ok(order);
        })
        .WithName("CancelOrder")
        .WithOpenApi();

        // Reschedule order
        group.MapPost("/{id}/reschedule", async (
            Guid id,
            [FromBody] RescheduleOrderDto dto,
            [FromServices] IOrderService orderService) =>
        {
            var order = await orderService.RescheduleOrderAsync(id, dto.NewScheduledDateTime, dto.Notes);
            return Results.Ok(order);
        })
        .WithName("RescheduleOrder")
        .WithOpenApi();

        // Approve order
        group.MapPost("/{id}/approve", async (
            Guid id,
            [FromBody] ApproveOrderDto dto,
            [FromServices] IOrderApprovalService approvalService,
            [FromServices] IOrderService orderService,
            [FromServices] IIdentityServiceClient identityServiceClient,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            Guid? approvedByUserId = null;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                approvedByUserId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                if (testClient != null)
                {
                    approvedByUserId = testClient.Id;
                }
            }

            var order = await approvalService.ApproveOrderAsync(id, dto.Reason, approvedByUserId);

            // Fire order approved notification to the client (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("NotificationService");
                    var fullOrder = await orderService.GetOrderByIdAsync(id);
                    if (fullOrder != null)
                    {
                        var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                        var clientUser = await userManager.FindByIdAsync(fullOrder.ClientId.ToString());
                        var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                            ? await identityServiceClient.GetUserByIdAsync(fullOrder.ClientId, accessToken)
                            : null;

                        var targetEmail = identityClientUser?.Email ?? clientUser?.Email;

                        var payload = JsonSerializer.Serialize(new
                        {
                            userId = fullOrder.ClientId,
                            userName = identityClientUser?.UserName ?? clientUser?.UserName ?? "Patient",
                            email = targetEmail,
                            orderId = fullOrder.Id,
                            doctorName = "Doctor",
                            appointmentDate = fullOrder.ScheduledDateTime.ToString("yyyy-MM-dd"),
                            appointmentTime = fullOrder.ScheduledDateTime.ToString("HH:mm"),
                            title = fullOrder.Title ?? "Appointment",
                            status = "Approved",
                            reason = dto.Reason
                        });
                        await client.PostAsJsonAsync("/api/notifications/events", new
                        {
                            sourceService = "AppointmentService",
                            eventName = "OrderApproved",
                            payload
                        });
                    }
                }
                catch { /* non-critical */ }
            });

            return Results.Ok(order);
        })
        .WithName("ApproveOrder")
        .WithOpenApi();

        // Decline order
        group.MapPost("/{id}/decline", async (
            Guid id,
            [FromBody] DeclineOrderDto dto,
            [FromServices] IOrderApprovalService approvalService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            Guid? declinedByUserId = null;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                declinedByUserId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                if (testClient != null)
                {
                    declinedByUserId = testClient.Id;
                }
            }

            var order = await approvalService.DeclineOrderAsync(id, dto.Reason, declinedByUserId);
            return Results.Ok(order);
        })
        .WithName("DeclineOrder")
        .WithOpenApi();

        // Complete order
        group.MapPost("/{id}/complete", async (
            Guid id,
            [FromBody] CompleteOrderDto? dto,
            [FromServices] IOrderApprovalService approvalService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            Guid? completedByUserId = null;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                completedByUserId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                // Use the test client from seed data
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                if (testClient != null)
                {
                    completedByUserId = testClient.Id;
                }
            }

            var order = await approvalService.CompleteOrderAsync(id, dto?.Notes, completedByUserId);
            return Results.Ok(order);
        })
        .WithName("CompleteOrder")
        .WithOpenApi();

        // Mark as no-show
        group.MapPost("/{id}/noshow", async (
            Guid id,
            [FromBody] NoShowOrderDto? dto,
            [FromServices] IOrderApprovalService approvalService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            Guid? markedByUserId = null;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                markedByUserId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                if (testClient != null)
                {
                    markedByUserId = testClient.Id;
                }
            }

            var order = await approvalService.MarkAsNoShowAsync(id, dto?.Notes, markedByUserId);
            return Results.Ok(order);
        })
        .WithName("MarkAsNoShow")
        .WithOpenApi();

        // Get order history
        group.MapGet("/{id}/history", async (
            Guid id,
            [FromServices] IOrderApprovalService approvalService) =>
        {
            var history = await approvalService.GetOrderHistoryAsync(id);
            return Results.Ok(history);
        })
        .WithName("GetOrderHistory")
        .WithOpenApi();

        // Delete order
        group.MapDelete("/{id}", async (
            Guid id,
            [FromServices] IOrderService orderService) =>
        {
            var result = await orderService.DeleteOrderAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteOrder")
        .WithOpenApi();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("nameid");

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private static Guid? ResolveUserId(HttpContext context)
    {
        var fromClaims = TryGetUserId(context.User);
        if (fromClaims.HasValue)
        {
            return fromClaims;
        }

        if (context.Request.Headers.TryGetValue("X-User-Id", out var headerValues))
        {
            var headerUserId = headerValues.FirstOrDefault();
            if (Guid.TryParse(headerUserId, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

public class CancelOrderDto
{
    public string? Reason { get; set; }
}

public class CompleteOrderDto
{
    public string? Notes { get; set; }
}

public class NoShowOrderDto
{
    public string? Notes { get; set; }
}