using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentApp.API.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        // Create order
        group.MapPost("/", async (
            [FromBody] CreateOrderDto dto,
            [FromServices] IOrderService orderService,
            HttpContext context) =>
        {
            var clientId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
            
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
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var orders = await orderService.GetOrdersByClientAsync(clientId, status, page, pageSize);
            return Results.Ok(orders);
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
            HttpContext context) =>
        {
            var cancelledByUserId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? "");
            var order = await orderService.CancelOrderAsync(id, dto?.Reason, cancelledByUserId);
            return Results.Ok(order);
        })
        .WithName("CancelOrder")
        .WithOpenApi();

        // Approve order
        group.MapPost("/{id}/approve", async (
            Guid id,
            [FromBody] ApproveOrderDto dto,
            [FromServices] IOrderApprovalService approvalService,
            HttpContext context) =>
        {
            var approvedByUserId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? "");
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
            HttpContext context) =>
        {
            var declinedByUserId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? "");
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
            HttpContext context) =>
        {
            var completedByUserId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? "");
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
            HttpContext context) =>
        {
            var markedByUserId = Guid.Parse(context.User.FindFirst("sub")?.Value ?? "");
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