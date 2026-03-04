# Appointment App - Core Feature Logic Analysis

## Project Overview

This is a **microservices-based appointment booking platform** with domain-agnostic architecture that supports multiple professional domains (medical, legal, consulting, etc.).

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 9.0, Minimal APIs |
| Database | PostgreSQL with Entity Framework Core |
| Authentication | JWT (Microsoft Identity) |
| Real-time Communication | SignalR |
| Containerization | Docker + Docker Compose |
| Async Processing | Task.Run for non-blocking operations |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (React)                         │
│                    - Appointment Booking UI                      │
│                    - Doctor Dashboard                            │
└──────────────────────────────┬──────────────────────────────────┘
                               │ HTTPS + JWT
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                     API Gateway / Nginx                          │
│                  - CORS, Rate Limiting, Routing                  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   Identity   │      │ Appointment  │      │ Notification │
│   Service    │      │   Service    │      │   Service    │
│  (Auth)      │      │  (Core)      │      │  (Events)    │
└──────┬───────┘      └──────┬───────┘      └──────┬───────┘
       │                     │                     │
       │                     │                     │
       ▼                     ▼                     ▼
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│   PostgreSQL │      │   PostgreSQL │      │   PostgreSQL │
│  (Users)     │      │  (Orders)    │      │ (Notifications)│
└──────────────┘      └──────────────┘      └──────────────┘
```

---

## 4-Layer Architecture

### 1. API Layer (`AppointmentApp.API`)
- **Purpose**: Minimal API endpoints, authentication, real-time updates
- **Key Components**:
  - JWT Authentication middleware
  - SignalR Hubs for real-time notifications
  - HTTP clients for inter-service communication
  - CORS configuration

### 2. Service Layer (`AppointmentApp.Service`)
- **Purpose**: Business logic, validation, orchestration
- **Key Components**:
  - `OrderService` - Order creation, modification, lifecycle
  - `OrderApprovalService` - State transitions, audit trail
  - `AvailabilityService` - Time slot management
  - `ProfessionalService` - Professional profile management

### 3. Repository Layer (`AppointmentApp.Repository`)
- **Purpose**: Data access abstraction
- **Key Components**:
  - Interfaces for all repositories
  - `IUnitOfWork` pattern for transaction management

### 4. Data Layer (`AppointmentApp.Postgres`)
- **Purpose**: Database operations, migrations
- **Key Components**:
  - Entity Framework Core DbContext
  - PostgreSQL-specific implementations
  - Database migrations

---

## Core Order Booking Flow

```
1. Client clicks "Book Appointment"
   ↓
2. Frontend POST /api/orders 
   { professionalId, scheduledDateTime, durationMinutes }
   ↓
3. Appointment Service - OrderService.CreateOrderAsync()
   ├─ Validate client exists (create shadow user if needed)
   ├─ Validate professional is available
   ├─ Check time slot availability
   └─ Create Order with Status = Requested
   ↓
4. Async Background Task (non-blocking)
   ├─ DocumentService.GenerateBookingPDF()
   │  └─ Returns documentId + downloadUrl
   ├─ NotificationService.EmitEvent("OrderCreated")
   │  └─ Triggers email + in-app notification to doctor
   └─ NotificationService.SendToClient("Booking Pending")
   ↓
5. Doctor receives notification via SignalR Hub
   ↓
6. Doctor clicks "Approve"
   ↓
7. Frontend POST /api/orders/{id}/approve { reason }
   ↓
8. Appointment Service - OrderApprovalService.ApproveOrderAsync()
   ├─ Validate state: Requested → Approved
   ├─ Update Order status
   └─ Create OrderHistory audit entry
   ↓
9. Async Background Task
   ├─ DocumentService.SendConfirmationEmail()
   │  └─ Email client with final booking PDF
   └─ NotificationService.EmitEvent("BookingConfirmed")
      └─ SignalR pushes to client dashboard
   ↓
10. Client receives confirmation email + dashboard update
```

---

## Order State Machine

```
           ┌─────────────────────────────────┐
           │         Created                 │
           │    (Status = Requested)         │
           └─────────────┬───────────────────┘
                         │
              ┌──────────┴──────────┐
              │                     │
              ▼                     ▼
    ┌──────────────────┐   ┌──────────────────┐
    │     Approve()    │   │    Decline()     │
    │                  │   │                  │
    ▼                  │   ▼                  │
┌──────────────┐       │  ┌──────────────┐   │
│   Approved   │       │  │   Declined   │   │
└──────┬───────┘       │  └──────────────┘   │
       │               │                      │
       │               │                      │
       │               ▼                      │
       │      ┌──────────────────┐           │
       │      │   Cancel()       │           │
       │      │ (from Requested) │           │
       │      └──────────────────┘           │
       │               │                      │
       │               │                      │
       ▼               │                      ▼
┌──────────────┐       │              ┌──────────────┐
│  Complete()  │       │              │  Cancelled   │
│              │       │              │              │
└──────────────┘       │              └──────────────┘
       │               │
       ▼               │
┌──────────────┐       │
│   Completed  │       │
└──────────────┘       │
                      │
                      │
                      ▼
              ┌──────────────┐
              │   Cancel()   │
              │(from Approved)│
              └──────────────┘
```

### Valid State Transitions

| From | To | Description |
|------|-----|-------------|
| Requested | Approved | Doctor accepts the booking |
| Requested | Declined | Doctor rejects the booking |
| Requested | Cancelled | Client or system cancels |
| Approved | Completed | Appointment finished successfully |
| Approved | Cancelled | Appointment cancelled after approval |

---

## Key Design Patterns

### 1. Repository Pattern

```csharp
// Interface (Domain/Repository)
IOrderRepository
    ├─ GetByIdAsync(Guid id)
    ├─ GetAllAsync(...)
    ├─ CreateAsync(Order order)
    ├─ UpdateAsync(Order order)
    └─ DeleteAsync(Guid id)

// Implementation (Data Layer)
OrderRepository : IOrderRepository
    └─ Uses AppointmentDbContext (EF Core)

// Usage (Service Layer)
_orderRepository.CreateAsync(order);
```

### 2. Service Layer Pattern

```csharp
// Domain Interface
IOrderService
    └─ CreateOrderAsync(...)

// Business Logic Implementation
OrderService : IOrderService
    ├─ Validates business rules
    ├─ Coordinates multiple repositories
    └─ Returns domain entities

// Registration (Program.cs)
builder.Services.AddScoped<IOrderService, OrderService>();
```

### 3. Event-Driven Architecture

```csharp
// Service A emits event
await notificationClient.PostAsJsonAsync("/api/notifications/events", new
{
    sourceService = "AppointmentService",
    eventName = "OrderCreated",
    payload = JsonSerializer.Serialize(orderData)
});

// Service B consumes event
→ Triggers email notifications
→ Sends SignalR real-time updates
→ Logs to audit system
```

### 4. State Machine Pattern

```csharp
// Enum defines valid states
public enum OrderStatus
{
    Requested = 0,
    Approved = 1,
    Declined = 2,
    Cancelled = 3,
    Completed = 4,
    NoShow = 5
}

// Service validates transitions
if (order.Status != OrderStatus.Requested)
{
    throw new InvalidOperationException(
        $"Cannot approve order with status {order.Status}");
}

// Audit trail tracks all changes
await CreateOrderHistoryAsync(orderId, fromStatus, toStatus, userId, notes);
```

---

## Code Examples

### 1. Order Entity (Domain Layer)

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace AppointmentApp.Domain.Entity;

/// <summary>
/// Core Order entity for appointment booking across all domains (medical, legal, consulting)
/// Follows state machine: Requested → Approved/Declined → Completed/Cancelled
/// </summary>
public class Order
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    
    public Guid ClientId { get; set; }
    public Guid ProfessionalId { get; set; }
    public Guid? DomainConfigurationId { get; set; }
    
    // Domain type: Medical, Legal, Consulting, etc.
    public DomainType DomainType { get; set; }
    
    // Order lifecycle state
    public OrderStatus Status { get; set; } = OrderStatus.Requested;
    
    // Scheduling
    public DateTime ScheduledDateTime { get; set; }
    public int DurationMinutes { get; set; }
    
    // Order details
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? DeclineReason { get; set; }
    public string? ApprovalReason { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Reference to pre-order collected data
    public Guid? PreOrderDataId { get; set; }

    // Navigation properties (lazy loading with EF Core)
    public AppIdentityUser? Client { get; set; }
    public AppIdentityUser? Professional { get; set; }
    public DomainConfiguration? DomainConfiguration { get; set; }
    public PreOrderData? PreOrderData { get; set; }
    public ICollection<OrderHistory> OrderHistory { get; set; } = new List<OrderHistory>();
}
```

### 2. Order Service - Core Business Logic

```csharp
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AppointmentApp.Service.Services;

/// <summary>
/// Core service for order management with validation and availability checking
/// Implements business rules for order creation, modification, and lifecycle
/// </summary>
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

    /// <summary>
    /// Creates a new order with full validation:
    /// 1. Ensure client exists (create shadow user if not)
    /// 2. Verify professional is available
    /// 3. Check time slot availability
    /// 4. Persist with status=Requested
    /// </summary>
    public async Task<Order> CreateOrderAsync(
        Guid clientId, 
        Guid professionalId, 
        DateTime scheduledDateTime, 
        int durationMinutes, 
        string? title = null, 
        string? description = null, 
        Guid? domainConfigurationId = null)
    {
        // Normalize all dates to UTC for consistency
        var normalizedScheduledDateTime = NormalizeToUtc(scheduledDateTime);

        // Ensure client user exists in local database
        // Creates shadow user if client only exists in Identity Service
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

        // Validate professional exists and is available
        var professional = await _professionalRepository.GetByIdAsync(professionalId);
        if (professional == null)
        {
            throw new ArgumentException("Professional not found", nameof(professionalId));
        }

        if (!professional.IsAvailable)
        {
            throw new InvalidOperationException("Professional is not available for booking");
        }

        // Check if the requested time slot is actually available
        var isAvailable = await _availabilitySlotRepository.IsSlotAvailableAsync(
            professionalId, 
            normalizedScheduledDateTime, 
            durationMinutes);
        
        if (!isAvailable)
        {
            throw new InvalidOperationException("Requested time slot is not available");
        }

        // Create and persist the order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ProfessionalId = professional.UserId,
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

    /// <summary>
    /// Cancels an order with state validation
    /// Only orders in Requested or Approved status can be cancelled
    /// Releases availability slots if order was Approved
    /// </summary>
    public async Task<Order> CancelOrderAsync(
        Guid orderId, 
        string? reason = null, 
        Guid? cancelledByUserId = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        var previousStatus = order.Status;

        // State machine validation
        if (order.Status != OrderStatus.Requested && order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot cancel order with status {order.Status}");
        }

        // Release reserved slots if order was previously approved
        if (previousStatus == OrderStatus.Approved)
        {
            await ReleaseReservedSlotsAsync(order.ProfessionalId, order.ScheduledDateTime, order.DurationMinutes);
        }

        order.Status = OrderStatus.Cancelled;
        order.Notes = reason;
        order.UpdatedAt = DateTime.UtcNow;

        return await _orderRepository.UpdateAsync(order);
    }

    private static DateTime NormalizeToUtc(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc 
            ? dateTime 
            : dateTime.Kind == DateTimeKind.Local 
                ? dateTime.ToUniversalTime() 
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    // Additional methods omitted for brevity...
}
```

### 3. Order Approval Service - State Transitions

```csharp
using AppointmentApp.Domain.Entity;
using AppointmentApp.Domain.Enums;
using AppointmentApp.Domain.Interfaces;
using AppointmentApp.Repository.Interfaces;

namespace AppointmentApp.Service.Services;

/// <summary>
/// Handles order state transitions: Approved, Declined, Completed, NoShow
/// Maintains audit trail via OrderHistory
/// </summary>
public class OrderApprovalService : IOrderApprovalService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderHistoryRepository _orderHistoryRepository;

    public OrderApprovalService(
        IOrderRepository orderRepository,
        IOrderHistoryRepository orderHistoryRepository)
    {
        _orderRepository = orderRepository;
        _orderHistoryRepository = orderHistoryRepository;
    }

    /// <summary>
    /// Approves an order: Requested → Approved
    /// Records history, sets completion timestamp expectation
    /// </summary>
    public async Task<Order> ApproveOrderAsync(Guid orderId, string? reason, Guid? approvedByUserId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new ArgumentException("Order not found", nameof(orderId));
        }

        // Validate state transition
        if (order.Status != OrderStatus.Requested)
        {
            throw new InvalidOperationException($"Cannot approve order with status {order.Status}");
        }

        var previousStatus = order.Status;
        
        // Perform state transition
        order.Status = OrderStatus.Approved;
        order.ApprovalReason = reason;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);

        // Record in audit history
        await CreateOrderHistoryAsync(orderId, previousStatus, OrderStatus.Approved, 
            approvedByUserId, $"Order approved. Reason: {reason ?? "None"}");

        return updatedOrder;
    }

    /// <summary>
    /// Creates an OrderHistory entry for audit trail
    /// Tracks all state changes with actor and timestamp
    /// </summary>
    private async Task CreateOrderHistoryAsync(
        Guid orderId,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        Guid? changedByUserId,
        string notes)
    {
        var history = new OrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        };

        await _orderHistoryRepository.CreateAsync(history);
    }

    // Additional state transition methods omitted for brevity...
}
```

### 4. API Endpoint - Order Creation with Event Integration

```csharp
using AppointmentApp.API.DTOs;
using AppointmentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AppointmentApp.API.Endpoints;

/// <summary>
/// Minimal API endpoints for Order management
/// Demonstrates event-driven architecture with async notification/document generation
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        /// <summary>
        /// POST /api/orders - Create a new booking order
        /// </summary>
        group.MapPost("/", async (
            [FromBody] CreateOrderDto dto,
            [FromServices] IOrderService orderService,
            [FromServices] IHttpClientFactory httpClientFactory,
            HttpContext context) =>
        {
            // Step 1: Authenticate user from JWT
            var clientId = ResolveUserId(context);
            if (!clientId.HasValue)
            {
                return Results.Unauthorized();
            }

            // Step 2: Create order with business logic validation
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

            // Step 3: Async background tasks (non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                    
                    // 3a. Generate booking document
                    Guid? bookingDocumentId = null;
                    string? bookingDocumentDownloadUrl = null;

                    try
                    {
                        var documentClient = httpClientFactory.CreateClient("DocumentService");
                        AddInternalServiceKey(documentClient, configuration);

                        var bookingDocumentRequest = new
                        {
                            orderId = order.Id,
                            patientName = $"{context.User.FindFirstValue(ClaimTypes.GivenName)} {context.User.FindFirstValue(ClaimTypes.Surname)}",
                            patientEmail = context.User.FindFirstValue(ClaimTypes.Email),
                            doctorName = ExtractDoctorNameFromOrderTitle(order.Title),
                            appointmentDate = order.ScheduledDateTime.ToString("yyyy-MM-dd"),
                            appointmentTime = order.ScheduledDateTime.ToString("HH:mm"),
                            status = "Pending"
                        };

                        var docResponse = await documentClient.PostAsJsonAsync(
                            "/api/documents/bookings/internal/generate",
                            bookingDocumentRequest);

                        if (docResponse.IsSuccessStatusCode)
                        {
                            var generated = await docResponse.Content.ReadFromJsonAsync<BookingDocumentResponse>();
                            bookingDocumentId = generated?.DocumentId;
                            bookingDocumentDownloadUrl = generated?.DownloadUrl;
                        }
                    }
                    catch
                    {
                        // Document generation failure is non-critical
                    }

                    // 3b. Emit event for Notification Service
                    var notificationClient = httpClientFactory.CreateClient("NotificationService");
                    var orderCreatedPayload = JsonSerializer.Serialize(new
                    {
                        professionalId = order.ProfessionalId,
                        clientId = clientId.Value,
                        orderId = order.Id,
                        patientName = context.User.FindFirstValue(ClaimTypes.Email),
                        appointmentDate = dto.ScheduledDateTime.ToString("yyyy-MM-dd"),
                        appointmentTime = dto.ScheduledDateTime.ToString("HH:mm"),
                        scheduledDateTime = order.ScheduledDateTime,
                        bookingDocumentId,
                        bookingDocumentDownloadUrl
                    });

                    await notificationClient.PostAsJsonAsync("/api/notifications/events", new
                    {
                        sourceService = "AppointmentService",
                        eventName = "OrderCreated",
                        payload = orderCreatedPayload
                    });
                }
                catch (Exception ex)
                {
                    // Notification failures are non-critical - order already created
                }
            });

            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .WithName("CreateOrder")
        .WithOpenApi();
    }

    private static Guid? ResolveUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst("sub")?.Value 
                        ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static void AddInternalServiceKey(HttpClient client, IConfiguration configuration)
    {
        var internalKey = configuration["InternalService:ApiKey"];
        if (!string.IsNullOrWhiteSpace(internalKey))
        {
            client.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalKey);
        }
    }
}
```

---

## Key Services

### Appointment Service (Core)
- **Order Management Module**: Create, update, cancel, retrieve appointments
- **Availability & Schedule Module**: Define working days and time slots
- **Order Approval Module**: Approve or decline requests with reasons
- **Domain Configuration Module**: Configure who can receive orders
- **Pre-Order Data Collection Module**: Request preliminary data from clients
- **Order History & Audit Module**: Store full order history

### Identity Service
- Authentication with Microsoft Identity (OAuth2 / OpenID Connect)
- JWT token generation and validation
- Role-based access control (patient, doctor, jurist, admin)
- User profile management

### Notification Service
- Notification preferences (email, in-app, push)
- Event-driven notification delivery
- Custom notification timing (reminders, delays)
- Template management with dynamic placeholders

### Document Service
- Secure document storage with versioning
- Auto-generate documents based on UI fields
- Document access control
- PDF generation for booking confirmations

### Chat Service
- One-to-one chat linked to orders
- Message persistence with metadata
- Automated chat workflows
- File and document sharing

---

## Async Processing Strategy

The application uses **non-blocking async operations** for scalability:

```csharp
// Fire-and-forget pattern for non-critical operations
_ = Task.Run(async () =>
{
    try
    {
        // Generate documents
        await documentService.GeneratePDF();
        
        // Send notifications
        await notificationService.SendEmail();
        
        // Emit events
        await eventBus.Publish("OrderCreated");
    }
    catch
    {
        // Log error but don't affect main response
    }
});

// Main request completes immediately
return Results.Created($"/api/orders/{order.Id}", order);
```

**Benefits:**
- Fast API response times
- Document generation doesn't block booking
- Notification failures don't prevent order creation
- Better user experience

---

## Inter-Service Communication

### HTTP Client Pattern
```csharp
// Register HttpClient in Program.cs
builder.Services.AddHttpClient("NotificationService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5003");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Inject and use in services
var client = _httpClientFactory.CreateClient("NotificationService");
await client.PostAsJsonAsync("/api/notifications/events", payload);
```

### Internal Service Authentication
```csharp
// Add API key header for inter-service calls
client.DefaultRequestHeaders.Add("X-Internal-Service-Key", internalKey);
```

---

## Database Schema

### Orders Table
```sql
CREATE TABLE Orders (
    Id UUID PRIMARY KEY,
    ClientId UUID NOT NULL,
    ProfessionalId UUID NOT NULL,
    DomainConfigurationId UUID,
    DomainType INT NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    ScheduledDateTime TIMESTAMP WITH TIME ZONE NOT NULL,
    DurationMinutes INT NOT NULL,
    Title VARCHAR(255),
    Description TEXT,
    Notes TEXT,
    DeclineReason TEXT,
    ApprovalReason TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE,
    CompletedAt TIMESTAMP WITH TIME ZONE,
    PreOrderDataId UUID
);
```

### OrderHistory Table (Audit Trail)
```sql
CREATE TABLE OrderHistory (
    Id UUID PRIMARY KEY,
    OrderId UUID NOT NULL,
    FromStatus INT NOT NULL,
    ToStatus INT NOT NULL,
    ChangedByUserId UUID,
    ChangedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    Notes TEXT,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);
```

---

## Security Features

1. **JWT Authentication**: Tokens issued by Identity Service
2. **Role-Based Authorization**: Patients, doctors, admins have different permissions
3. **Internal Service Keys**: Secure communication between microservices
4. **Input Validation**: All DTOs validated before processing
5. **SQL Injection Prevention**: EF Core parameterized queries
6. **CORS Policies**: Controlled frontend access

---

## Summary

This appointment booking platform demonstrates:

✅ **Clean Architecture** - Clear separation of concerns with 4-layer design  
✅ **Microservices** - Independent services with HTTP communication  
✅ **Event-Driven** - Async events for notifications and documents  
✅ **State Machine** - Validated order lifecycle transitions  
✅ **Audit Trail** - Complete history of all state changes  
✅ **Scalability** - Non-blocking async operations for performance  
✅ **Security** - JWT auth, RBAC, internal service authentication  
✅ **Domain Agnostic** - Supports medical, legal, consulting domains  

The implementation follows SOLID principles and industry best practices for building scalable, maintainable enterprise applications.