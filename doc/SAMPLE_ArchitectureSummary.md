# Appointment App - Core Feature Logic Analysis

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

## Core Order Booking Flow

```
1. Client clicks "Book Appointment"
   ↓
2. Frontend POST /api/orders { professionalId, scheduledDateTime, durationMinutes }
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

## State Machine

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

## Key Design Patterns

### 1. Repository Pattern
```csharp
IOrderRepository (Interface)
    ↓
OrderRepository (EF Core Implementation)
    ↓
AppointmentDbContext (PostgreSQL)
```

### 2. Service Layer Pattern
```csharp
IOrderService (Domain Interface)
    ↓
OrderService (Business Logic)
    ↓ Uses
IOrderRepository + IProfessionalRepository + IAvailabilitySlotRepository
```

### 3. Event-Driven Architecture
```csharp
Appointment Service
    ↓ HTTP POST /events
Notification Service
    ↓ SignalR + Email + Push
Clients (Real-time updates)
```

### 4. State Machine Pattern
```csharp
OrderStatus Enum + Validation in Service Layer
Only valid transitions allowed:
- Requested → Approved
- Requested → Declined
- Requested → Cancelled
- Approved → Completed
- Approved → Cancelled
```

## Key Technologies

- **Backend**: .NET 9.0, Minimal APIs
- **Database**: PostgreSQL with EF Core
- **Authentication**: JWT (Microsoft Identity)
- **Real-time**: SignalR
- **Async Tasks**: Task.Run for non-blocking operations
- **Microservices**: HTTP communication between services
- **Containerization**: Docker + Docker Compose

## Sample Files Created

1. `SAMPLE_OrderEntity.cs` - Domain entity with state machine
2. `SAMPLE_OrderService.cs` - Core business logic with validation
3. `SAMPLE_OrderApprovalService.cs` - State transitions and audit trail
4. `SAMPLE_OrderEndpoint.cs` - API endpoints with event integration