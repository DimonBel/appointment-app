using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppointmentApp.API.Endpoints;

public static class PreOrderDataEndpoints
{
    public static void MapPreOrderDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pre-order-data")
            .WithTags("PreOrderData");
        // .RequireAuthorization(); // Temporarily disabled for testing

        // Create pre-order data for an order
        group.MapPost("/", async (
            [FromBody] CreatePreOrderDataDto dto,
            [FromServices] IPreOrderDataService preOrderDataService,
            HttpContext context) =>
        {
            var clientId = ResolveUserId(context) ?? dto.ClientId;
            var preOrderData = await preOrderDataService.CreatePreOrderDataAsync(
                dto.OrderId, clientId, dto.DataFields);
            return Results.Created($"/api/pre-order-data/{preOrderData.Id}", preOrderData);
        })
        .WithName("CreatePreOrderData")
        .WithOpenApi();

        // Get pre-order data by order ID
        group.MapGet("/order/{orderId:guid}", async (
            Guid orderId,
            [FromServices] IPreOrderDataService preOrderDataService) =>
        {
            var data = await preOrderDataService.GetPreOrderDataByOrderIdAsync(orderId);
            return data != null ? Results.Ok(data) : Results.NotFound();
        })
        .WithName("GetPreOrderDataByOrderId")
        .WithOpenApi();

        // Update pre-order data
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdatePreOrderDataDto dto,
            [FromServices] IPreOrderDataService preOrderDataService) =>
        {
            try
            {
                var updated = await preOrderDataService.UpdatePreOrderDataAsync(id, dto.DataFields);
                return Results.Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("UpdatePreOrderData")
        .WithOpenApi();

        // Mark pre-order data as completed
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            [FromServices] IPreOrderDataService preOrderDataService) =>
        {
            try
            {
                var completed = await preOrderDataService.MarkAsCompletedAsync(id);
                return Results.Ok(completed);
            }
            catch (ArgumentException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .WithName("MarkPreOrderDataAsCompleted")
        .WithOpenApi();

        // Validate pre-order data against required fields
        group.MapPost("/{id:guid}/validate", async (
            Guid id,
            [FromBody] Dictionary<string, string> requiredFields,
            [FromServices] IPreOrderDataService preOrderDataService) =>
        {
            var isValid = await preOrderDataService.ValidatePreOrderDataAsync(id, requiredFields);
            return Results.Ok(new { isValid });
        })
        .WithName("ValidatePreOrderData")
        .WithOpenApi();

        // Delete pre-order data
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] IPreOrderDataService preOrderDataService) =>
        {
            var result = await preOrderDataService.DeletePreOrderDataAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeletePreOrderData")
        .WithOpenApi();
    }

    private static Guid? ResolveUserId(HttpContext context)
    {
        var claimValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue("nameid");
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}

public class CreatePreOrderDataDto
{
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public Dictionary<string, string> DataFields { get; set; } = new();
}

public class UpdatePreOrderDataDto
{
    public Dictionary<string, string> DataFields { get; set; } = new();
}
