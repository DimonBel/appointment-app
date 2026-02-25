using AppointmentApp.API.DTOs;
using Identity.API.DTOs;
using AppointmentApp.API.Services;
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppointmentApp.API.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        // Get all orders (for management panel)
        group.MapGet("/all", async (
            [FromServices] IOrderService orderService,
            [FromServices] IIdentityServiceClient identityServiceClient,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool descending = false) =>
        {
            var orders = await orderService.GetAllOrdersAsync(status, page, pageSize, sortBy, descending);
            
            // Enrich orders with Identity service data
            var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            var enrichedOrders = orders.Select(order =>
            {
                var orderDict = new Dictionary<string, object?>
                {
                    ["id"] = order.Id,
                    ["clientId"] = order.ClientId,
                    ["professionalId"] = order.ProfessionalId,
                    ["status"] = order.Status,
                    ["scheduledDateTime"] = order.ScheduledDateTime,
                    ["durationMinutes"] = order.DurationMinutes,
                    ["title"] = order.Title,
                    ["description"] = order.Description,
                    ["notes"] = order.Notes,
                    ["createdAt"] = order.CreatedAt,
                    ["updatedAt"] = order.UpdatedAt,
                    ["client"] = null,
                    ["professional"] = null
                };

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var identityClient = identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken);
                    var identityProfessional = identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken);
                    
                    Task.WaitAll(identityClient, identityProfessional);
                    
                    orderDict["client"] = identityClient.Result;
                    orderDict["professional"] = identityProfessional.Result;
                }

                return orderDict;
            }).ToList();
            
            return Results.Ok(enrichedOrders);
        })
        .WithName("GetAllOrdersForManagement")
        .WithOpenApi();

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

            Order order;
            try
            {
                order = await orderService.CreateOrderAsync(
                    clientId.Value,
                    dto.ProfessionalId,
                    dto.ScheduledDateTime,
                    dto.DurationMinutes,
                    dto.Title,
                    dto.Description,
                    dto.DomainConfigurationId);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return Results.BadRequest(new { message = message });
            }

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
                    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();

                    string? bookingDocumentDownloadUrl = null;
                    Guid? bookingDocumentId = null;

                    try
                    {
                        var documentClient = httpClientFactory.CreateClient("DocumentService");
                        AddInternalServiceKey(documentClient, configuration);

                        var bookingDocumentRequest = BuildBookingDocumentRequest(
                            order,
                            patientName,
                            userEmail,
                            ExtractDoctorNameFromOrderTitle(order.Title) ?? "Doctor",
                            status: "Pending");

                        var docResponse = await documentClient.PostAsJsonAsync(
                            "/api/documents/bookings/internal/generate",
                            bookingDocumentRequest);

                        if (docResponse.IsSuccessStatusCode)
                        {
                            var generated = await docResponse.Content.ReadFromJsonAsync<BookingDocumentResponse>();
                            bookingDocumentId = generated?.DocumentId;
                            bookingDocumentDownloadUrl = BuildDocumentDownloadUrl(configuration, generated?.DownloadUrl);
                        }
                    }
                    catch
                    {
                        // non-critical
                    }

                    var client = httpClientFactory.CreateClient("NotificationService");
                    AddInternalServiceKey(client, configuration);
                    var orderCreatedPayload = JsonSerializer.Serialize(new
                    {
                        professionalId = order.ProfessionalId,
                        clientId = clientId.Value,
                        orderId = order.Id,
                        patientName,
                        appointmentDate = dto.ScheduledDateTime.ToString("yyyy-MM-dd"),
                        appointmentTime = dto.ScheduledDateTime.ToString("HH:mm"),
                        scheduledDateTime = dto.ScheduledDateTime,
                        bookingDocumentId,
                        bookingDocumentDownloadUrl
                    });

                    var eventResponse = await client.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "AppointmentService",
                        eventName = "OrderCreated",
                        payload = orderCreatedPayload
                    });

                    if (!eventResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await eventResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Failed to send OrderCreated event: {eventResponse.StatusCode}, {errorContent}");
                    }

                    var notificationResponse = await client.PostAsJsonAsync("/api/notifications", new
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

                    if (!notificationResponse.IsSuccessStatusCode)
                    {
                        var errorContent = await notificationResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"Failed to send booking notification: {notificationResponse.StatusCode}, {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending notifications: {ex.Message}");
                }
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

        // Generate booking document for an existing order
        group.MapPost("/{id}/booking-document/generate", async (
            Guid id,
            [FromServices] IOrderService orderService,
            [FromServices] IIdentityServiceClient identityServiceClient,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] UserManager<AppIdentityUser> userManager,
            HttpContext context) =>
        {
            var order = await orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return Results.NotFound(new { message = "Order not found." });
            }

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

            var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
            var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());

            var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                : null;
            var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                : null;

            var patientName = ResolveIdentityDisplayName(identityClientUser)
                ?? ResolveAppUserDisplayName(clientUser, "Patient");
            var patientEmail = ResolvePreferredEmail(identityClientUser?.Email, clientUser?.Email);
            var doctorName = ResolveIdentityDisplayName(identityProfessionalUser)
                ?? ExtractDoctorNameFromOrderTitle(order.Title)
                ?? ResolveAppUserDisplayName(professionalUser, "Doctor");

            var documentClient = httpClientFactory.CreateClient("DocumentService");
            AddInternalServiceKey(documentClient, configuration);

            var bookingDocumentRequest = BuildBookingDocumentRequest(
                order,
                patientName,
                patientEmail,
                doctorName,
                order.Status.ToString());

            var docResponse = await documentClient.PostAsJsonAsync(
                "/api/documents/bookings/internal/generate",
                bookingDocumentRequest);

            if (!docResponse.IsSuccessStatusCode)
            {
                return Results.BadRequest(new { message = "Failed to generate booking document." });
            }

            var generated = await docResponse.Content.ReadFromJsonAsync<BookingDocumentResponse>();
            if (generated == null || generated.DocumentId == Guid.Empty)
            {
                return Results.BadRequest(new { message = "Booking document generation returned an invalid response." });
            }

            return Results.Ok(new
            {
                documentId = generated.DocumentId,
                downloadUrl = BuildDocumentDownloadUrl(configuration, generated.DownloadUrl)
            });
        })
        .WithName("GenerateBookingDocument")
        .WithOpenApi();

        // Get orders by client
        group.MapGet("/client/{clientId}", async (
            Guid clientId,
            [FromServices] IOrderService orderService,
            [FromServices] IProfessionalRepository professionalRepo,
            [FromQuery] Guid? professionalId = null,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var orders = await orderService.GetOrdersByClientAsync(clientId, status, page, pageSize);
            if (professionalId.HasValue)
            {
                orders = orders.Where(o => o.ProfessionalId == professionalId.Value);
            }

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

        // Get clients by professional (for doctor panel)
        group.MapGet("/professional/{professionalId}/clients", async (
            Guid professionalId,
            [FromServices] IOrderRepository orderRepository,
            [FromServices] IIdentityServiceClient identityServiceClient,
            HttpContext context) =>
        {
            var clients = await orderRepository.GetClientsByProfessionalAsync(professionalId);
            
            // Enrich clients with Identity service data
            var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            var enrichedClients = new List<object>();

            foreach (var client in clients)
            {
                var clientDict = new Dictionary<string, object?>
                {
                    ["id"] = client.Id,
                    ["userName"] = client.UserName,
                    ["email"] = client.Email,
                    ["firstName"] = client.FirstName,
                    ["lastName"] = client.LastName,
                    ["phoneNumber"] = client.PhoneNumber,
                    ["avatarUrl"] = null,
                    ["isActive"] = client.IsActive,
                    ["isOnline"] = false,
                    ["createdAt"] = client.CreatedAt
                };

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    var identityUser = await identityServiceClient.GetUserByIdAsync(client.Id, accessToken);
                    if (identityUser != null)
                    {
                        clientDict["avatarUrl"] = identityUser.AvatarUrl;
                        clientDict["firstName"] = identityUser.FirstName ?? client.FirstName;
                        clientDict["lastName"] = identityUser.LastName ?? client.LastName;
                        clientDict["isOnline"] = identityUser.IsOnline;
                    }
                }

                enrichedClients.Add(clientDict);
            }
            
            return Results.Ok(enrichedClients);
        })
        .WithName("GetClientsByProfessional")
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

            Order order;
            try
            {
                order = await approvalService.ApproveOrderAsync(id, dto.Reason, approvedByUserId);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }

            try
            {
                var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                var client = httpClientFactory.CreateClient("NotificationService");
                AddInternalServiceKey(client, configuration);
                var documentClient = httpClientFactory.CreateClient("DocumentService");
                AddInternalServiceKey(documentClient, configuration);

                var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
                var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());
                var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                    : null;
                var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                    : null;

                var targetEmail = ResolvePreferredEmail(identityClientUser?.Email, clientUser?.Email);
                var actorUserId = TryGetUserId(context.User);
                var doctorNameFromClaims = actorUserId == order.ProfessionalId
                    ? ResolvePreferredDisplayName(ResolveDisplayName(context.User, string.Empty))
                    : null;
                var doctorNameFromTitle = ExtractDoctorNameFromOrderTitle(order.Title);
                var doctorName = ResolveIdentityDisplayName(identityProfessionalUser)
                    ?? doctorNameFromTitle
                    ?? doctorNameFromClaims
                    ?? ResolveAppUserDisplayName(professionalUser, "Doctor");

                string? bookingDocumentDownloadUrl = null;
                Guid? bookingDocumentId = null;

                if (!string.IsNullOrWhiteSpace(targetEmail))
                {
                    var emailRequest = new
                    {
                        booking = BuildBookingDocumentRequest(order, identityClientUser?.UserName ?? clientUser?.UserName ?? "Patient", targetEmail, doctorName, "Confirmed"),
                        recipientEmail = targetEmail
                    };

                    var documentEmailResponse = await documentClient.PostAsJsonAsync(
                        "/api/documents/bookings/internal/send-confirmation-email",
                        emailRequest);

                    if (documentEmailResponse.IsSuccessStatusCode)
                    {
                        var generated = await documentEmailResponse.Content.ReadFromJsonAsync<BookingDocumentResponse>();
                        bookingDocumentId = generated?.DocumentId;
                        bookingDocumentDownloadUrl = BuildDocumentDownloadUrl(configuration, generated?.DownloadUrl);
                    }
                }

                var payload = JsonSerializer.Serialize(new
                {
                    userId = order.ClientId,
                    userName = identityClientUser?.UserName ?? clientUser?.UserName ?? "Patient",
                    orderId = order.Id,
                    doctorName,
                    appointmentDate = order.ScheduledDateTime.ToString("yyyy-MM-dd"),
                    appointmentTime = order.ScheduledDateTime.ToString("HH:mm"),
                    title = order.Title ?? "Appointment",
                    status = "Approved",
                    reason = dto.Reason,
                    bookingDocumentId,
                    bookingDocumentDownloadUrl
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
                AddInternalServiceKey(client, context.RequestServices.GetRequiredService<IConfiguration>());
                var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
                var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());
                var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                    : null;
                var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                    : null;

                var targetEmail = ResolvePreferredEmail(identityClientUser?.Email, clientUser?.Email);
                var actorUserId = TryGetUserId(context.User);
                var doctorNameFromClaims = actorUserId == order.ProfessionalId
                    ? ResolvePreferredDisplayName(ResolveDisplayName(context.User, string.Empty))
                    : null;
                var doctorNameFromTitle = ExtractDoctorNameFromOrderTitle(order.Title);
                var doctorName = ResolveIdentityDisplayName(identityProfessionalUser)
                    ?? doctorNameFromTitle
                    ?? doctorNameFromClaims
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
                AddInternalServiceKey(client, context.RequestServices.GetRequiredService<IConfiguration>());
                var accessToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
                var clientUser = await userManager.FindByIdAsync(order.ClientId.ToString());
                var professionalUser = await userManager.FindByIdAsync(order.ProfessionalId.ToString());
                var identityClientUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ClientId, accessToken)
                    : null;
                var identityProfessionalUser = !string.IsNullOrWhiteSpace(accessToken)
                    ? await identityServiceClient.GetUserByIdAsync(order.ProfessionalId, accessToken)
                    : null;

                var targetEmail = ResolvePreferredEmail(identityClientUser?.Email, clientUser?.Email);
                var actorUserId = TryGetUserId(context.User);
                var doctorNameFromClaims = actorUserId == order.ProfessionalId
                    ? ResolvePreferredDisplayName(ResolveDisplayName(context.User, string.Empty))
                    : null;
                var doctorNameFromTitle = ExtractDoctorNameFromOrderTitle(order.Title);
                var doctorName = ResolveIdentityDisplayName(identityProfessionalUser)
                    ?? doctorNameFromTitle
                    ?? doctorNameFromClaims
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

    private static bool IsShadowEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        return email.EndsWith("@shadow.local", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolvePreferredEmail(string? primaryEmail, string? fallbackEmail)
    {
        if (!IsShadowEmail(primaryEmail))
        {
            return primaryEmail;
        }

        if (!IsShadowEmail(fallbackEmail))
        {
            return fallbackEmail;
        }

        return null;
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

    private static object BuildBookingDocumentRequest(Order order, string patientName, string? patientEmail, string doctorName, string status)
    {
        return new
        {
            orderId = order.Id,
            clientId = order.ClientId,
            doctorId = order.ProfessionalId,
            facilityName = "Healthcare Hub",
            facilityAddress = "Medical Center Address",
            facilityPhone = "+1 345-67-890",
            facilityEmail = "support@healthcarehub.local",
            facilityWebsite = "www.healthcarehub.local",
            bookingNumber = order.Id.ToString("N")[..8].ToUpperInvariant(),
            bookingDateUtc = DateTime.UtcNow,
            status,
            patientName,
            patientEmail = patientEmail ?? string.Empty,
            doctorName,
            scheduledDateTimeUtc = order.ScheduledDateTime,
            durationMinutes = order.DurationMinutes,
            taxRate = 0.075m,
            additionalInformation = "Please arrive 10 minutes before your scheduled appointment.",
            lineItems = new[]
            {
                new
                {
                    quantity = 1m,
                    description = order.Title ?? $"Consultation with {doctorName}",
                    unitPrice = 100m
                }
            }
        };
    }

    private static void AddInternalServiceKey(HttpClient client, IConfiguration configuration)
    {
        var key = configuration["InternalServiceKey"] ?? "internal-dev-key";
        client.DefaultRequestHeaders.Remove("X-Internal-Key");
        client.DefaultRequestHeaders.Add("X-Internal-Key", key);
    }

    private static string? BuildDocumentDownloadUrl(IConfiguration configuration, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (Uri.TryCreate(relativePath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        var publicBaseUrl = configuration["DocumentService:PublicBaseUrl"]
            ?? configuration["DocumentService:BaseUrl"]
            ?? "http://localhost:5004";

        // If PublicBaseUrl is empty, return the relative path as-is
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return relativePath.TrimStart('/');
        }

        return $"{publicBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}

internal class BookingDocumentResponse
{
    public Guid DocumentId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
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