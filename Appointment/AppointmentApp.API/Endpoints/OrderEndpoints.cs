using AppointmentApp.API.DTOs;
using AppointmentApp.API.DTOs.Identity;
using AppointmentApp.API.Services;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

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
            [FromServices] IIdentityServiceClient identityServiceClient,
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
            var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            var identityUser = !string.IsNullOrWhiteSpace(accessToken)
                ? await identityServiceClient.GetUserByIdAsync(clientId.Value, accessToken)
                : null;

            var patientName = ResolveIdentityDisplayName(identityUser)
                ?? ResolveDisplayName(context.User, localUser?.UserName ?? "Patient");
            var userEmail = context.User.FindFirstValue(ClaimTypes.Email)
                ?? context.User.FindFirstValue("email")
                ?? identityUser?.Email
                ?? localUser?.Email;

            // Fire booking request notification for doctor only.
            // Client confirmation is sent only after doctor approves.
            _ = Task.Run(async () =>
            {
                try
                {
                    var client = httpClientFactory.CreateClient("NotificationService");
                    var orderCreatedPayload = JsonSerializer.Serialize(new
                    {
                        professionalId = order.ProfessionalId,
                        clientId = clientId.Value,
                        orderId = order.Id,
                        patientName,
                        appointmentDate = dto.ScheduledDateTime.ToString("yyyy-MM-dd"),
                        appointmentTime = dto.ScheduledDateTime.ToString("HH:mm"),
                        scheduledDateTime = dto.ScheduledDateTime
                    });

                    await client.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "AppointmentService",
                        eventName = "OrderCreated",
                        payload = orderCreatedPayload
                    });

                    await client.PostAsJsonAsync("/api/notifications", new
                    {
                        userId = clientId.Value,
                        title = "Booking Pending",
                        message = "Your booking request has been sent and is pending doctor confirmation.",
                        type = 0,
                        referenceId = order.Id,
                        referenceType = "Order",
                        metadata = JsonSerializer.Serialize(new
                        {
                            status = "Pending",
                            appointmentDate = dto.ScheduledDateTime.ToString("yyyy-MM-dd"),
                            appointmentTime = dto.ScheduledDateTime.ToString("HH:mm")
                        })
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

            try
            {
                var order = await orderService.CancelOrderAsync(id, dto?.Reason, cancelledByUserId.Value);
                return Results.Ok(ToOrderStatusResponse(order));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
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

            try
            {
                var client = httpClientFactory.CreateClient("NotificationService");
                var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
                var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());
                var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                    : null;
                var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                    : null;

                var targetEmail = identityClientUser?.Email ?? clientUser?.Email;
                var actorUserId = TryGetUserId(context.User);
                var doctorNameFromClaims = actorUserId == order.ProfessionalId
                    ? ResolvePreferredDisplayName(ResolveDisplayName(context.User, string.Empty))
                    : null;
                var doctorNameFromTitle = ExtractDoctorNameFromOrderTitle(order.Title);
                var doctorName = doctorNameFromClaims
                    ?? ResolveIdentityDisplayName(identityProfessionalUser)
                    ?? doctorNameFromTitle
                    ?? ResolveAppUserDisplayName(professionalUser, "Doctor");

                var payload = JsonSerializer.Serialize(new
                {
                    userId = order.ClientId,
                    userName = identityClientUser?.UserName ?? clientUser?.UserName ?? "Patient",
                    email = targetEmail,
                    orderId = order.Id,
                    doctorName,
                    appointmentDate = order.ScheduledDateTime.ToString("yyyy-MM-dd"),
                    appointmentTime = order.ScheduledDateTime.ToString("HH:mm"),
                    title = order.Title ?? "Appointment",
                    status = "Approved",
                    reason = dto.Reason
                });

                await client.PostAsJsonAsync("/api/notifications/events", new
                {
                    sourceService = "AppointmentService",
                    eventName = "BookingConfirmed",
                    payload
                });
            }
            catch { /* non-critical */ }

            return Results.Ok(ToOrderStatusResponse(order));
        })
        .WithName("ApproveOrder")
        .WithOpenApi();

        // Decline order
        group.MapPost("/{id}/decline", async (
            Guid id,
            [FromBody] DeclineOrderDto dto,
            [FromServices] IOrderApprovalService approvalService,
            [FromServices] IIdentityServiceClient identityServiceClient,
            [FromServices] IHttpClientFactory httpClientFactory,
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

            try
            {
                var client = httpClientFactory.CreateClient("NotificationService");
                var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
                var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());
                var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                    : null;
                var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                    : null;

                var targetEmail = identityClientUser?.Email ?? clientUser?.Email;
                var actorUserId = TryGetUserId(context.User);
                var doctorNameFromClaims = actorUserId == order.ProfessionalId
                    ? ResolvePreferredDisplayName(ResolveDisplayName(context.User, string.Empty))
                    : null;
                var doctorNameFromTitle = ExtractDoctorNameFromOrderTitle(order.Title);
                var doctorName = doctorNameFromClaims
                    ?? ResolveIdentityDisplayName(identityProfessionalUser)
                    ?? doctorNameFromTitle
                    ?? ResolveAppUserDisplayName(professionalUser, "Doctor");

                var payload = JsonSerializer.Serialize(new
                {
                    userId = order.ClientId,
                    userName = identityClientUser?.UserName ?? clientUser?.UserName ?? "Patient",
                    email = targetEmail,
                    orderId = order.Id,
                    doctorName,
                    appointmentDate = order.ScheduledDateTime.ToString("yyyy-MM-dd"),
                    appointmentTime = order.ScheduledDateTime.ToString("HH:mm"),
                    title = order.Title ?? "Appointment",
                    status = "Declined",
                    reason = dto.Reason
                });

                await client.PostAsJsonAsync("/api/notifications/events", new
                {
                    sourceService = "AppointmentService",
                    eventName = "OrderDeclined",
                    payload
                });
            }
            catch { /* non-critical */ }

            return Results.Ok(ToOrderStatusResponse(order));
        })
        .WithName("DeclineOrder")
        .WithOpenApi();

        // Complete order
        group.MapPost("/{id}/complete", async (
            Guid id,
            [FromBody] CompleteOrderDto? dto,
            [FromServices] IOrderApprovalService approvalService,
            [FromServices] IIdentityServiceClient identityServiceClient,
            [FromServices] IHttpClientFactory httpClientFactory,
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

            try
            {
                var client = httpClientFactory.CreateClient("NotificationService");
                var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
                var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());
                var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                    : null;
                var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                    : null;

                var targetEmail = identityClientUser?.Email ?? clientUser?.Email;
                var actorUserId = TryGetUserId(context.User);
                var doctorNameFromClaims = actorUserId == order.ProfessionalId
                    ? ResolvePreferredDisplayName(ResolveDisplayName(context.User, string.Empty))
                    : null;
                var doctorNameFromTitle = ExtractDoctorNameFromOrderTitle(order.Title);
                var doctorName = doctorNameFromClaims
                    ?? ResolveIdentityDisplayName(identityProfessionalUser)
                    ?? doctorNameFromTitle
                    ?? ResolveAppUserDisplayName(professionalUser, "Doctor");

                var payload = JsonSerializer.Serialize(new
                {
                    userId = order.ClientId,
                    userName = identityClientUser?.UserName ?? clientUser?.UserName ?? "Patient",
                    email = targetEmail,
                    orderId = order.Id,
                    doctorName,
                    appointmentDate = order.ScheduledDateTime.ToString("yyyy-MM-dd"),
                    appointmentTime = order.ScheduledDateTime.ToString("HH:mm"),
                    title = order.Title ?? "Appointment",
                    status = "Completed",
                    reason = dto?.Notes
                });

                await client.PostAsJsonAsync("/api/notifications/events", new
                {
                    sourceService = "AppointmentService",
                    eventName = "OrderCompleted",
                    payload
                });
            }
            catch { /* non-critical */ }

            return Results.Ok(ToOrderStatusResponse(order));
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

    private static string ResolveDisplayName(ClaimsPrincipal user, string fallback)
    {
        var customFirstName = user.FindFirstValue("FirstName");
        var customLastName = user.FindFirstValue("LastName");
        var customFirstNameLower = user.FindFirstValue("firstName") ?? user.FindFirstValue("firstname");
        var customLastNameLower = user.FindFirstValue("lastName") ?? user.FindFirstValue("lastname");
        var claimFirstName = user.FindFirstValue(ClaimTypes.GivenName);
        var claimLastName = user.FindFirstValue(ClaimTypes.Surname);
        var jwtGivenName = user.FindFirstValue("given_name");
        var jwtFamilyName = user.FindFirstValue("family_name");

        var firstName = customFirstName ?? customFirstNameLower ?? claimFirstName ?? jwtGivenName;
        var lastName = customLastName ?? customLastNameLower ?? claimLastName ?? jwtFamilyName;

        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
        {
            return $"{firstName} {lastName}";
        }

        return user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue("name")
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? fallback;
    }

    private static string? ResolveIdentityDisplayName(IdentityUserDto? user)
    {
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            var preferredFullName = ResolvePreferredDisplayName(fullName);
            if (!string.IsNullOrWhiteSpace(preferredFullName))
            {
                return preferredFullName;
            }
        }

        return ResolvePreferredDisplayName(user.UserName);
    }

    private static string ResolveAppUserDisplayName(AppIdentityUser? user, string fallback)
    {
        if (user == null) return fallback;

        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (!string.Equals(fullName, "Doctor Profile", StringComparison.OrdinalIgnoreCase))
            {
                return fullName;
            }
        }

        var preferredUserName = ResolvePreferredDisplayName(user.UserName);
        if (!string.IsNullOrWhiteSpace(preferredUserName))
        {
            return preferredUserName;
        }

        return fallback;
    }

    private static string? ResolvePreferredDisplayName(string? rawDisplayName)
    {
        if (string.IsNullOrWhiteSpace(rawDisplayName)) return null;

        var value = rawDisplayName.Trim();
        if (value.Contains('@')) return null;
        if (string.Equals(value, "Doctor Profile", StringComparison.OrdinalIgnoreCase)) return null;
        if (string.Equals(value, "User Profile", StringComparison.OrdinalIgnoreCase)) return null;
        if (Regex.IsMatch(value, "^user_[0-9a-f]{16,}$", RegexOptions.IgnoreCase)) return null;

        return value;
    }

    private static string? ExtractDoctorNameFromOrderTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        const string marker = "Appointment with Dr.";
        if (!title.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var extracted = title.Substring(marker.Length).Trim();
        return ResolvePreferredDisplayName(extracted);
    }

    private static object ToOrderStatusResponse(Order order)
    {
        return new
        {
            order.Id,
            order.ClientId,
            order.ProfessionalId,
            order.Status,
            order.ScheduledDateTime,
            order.DurationMinutes,
            order.Title,
            order.Description,
            order.Notes,
            order.DeclineReason,
            order.ApprovalReason,
            order.CompletedAt,
            order.CreatedAt,
            order.UpdatedAt
        };
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