using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            // For testing without auth, use the seeded client user
            Guid userId;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                userId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                // Use the test client from seed data
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                if (testClient == null)
                {
                    return Results.BadRequest("Test client not found. Please ensure database is seeded.");
                }
                userId = testClient.Id;
            }

            var orders = await orderService.GetOrdersByClientAsync(userId, status, page, pageSize);
            return Results.Ok(orders);
        })
        .WithName("GetAllOrders")
        .WithOpenApi();

        // Create order
        group.MapPost("/", async (
            [FromBody] CreateOrderDto dto,
            [FromServices] IOrderService orderService,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            // For testing without auth, use the seeded client user
            Guid clientId;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                clientId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                // Use the test client from seed data
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                if (testClient == null)
                {
                    return Results.BadRequest("Test client not found. Please ensure database is seeded.");
                }
                clientId = testClient.Id;
            }

            var order = await orderService.CreateOrderAsync(
                clientId,
                dto.ProfessionalId,
                dto.ScheduledDateTime,
                dto.DurationMinutes,
                dto.Title,
                dto.Description,
                dto.DomainConfigurationId);

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
            Guid cancelledByUserId;
            if (context.User.FindFirst("sub")?.Value != null)
            {
                cancelledByUserId = Guid.Parse(context.User.FindFirst("sub").Value);
            }
            else
            {
                var testClient = await userManager.FindByEmailAsync("client@appointment.com");
                cancelledByUserId = testClient?.Id ?? Guid.Empty;
            }
            var order = await orderService.CancelOrderAsync(id, dto?.Reason, cancelledByUserId);
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