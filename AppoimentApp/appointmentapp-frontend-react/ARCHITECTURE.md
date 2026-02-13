## Application Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    APPOINTMENT SYSTEM                        │
│                     Frontend (React)                         │
└─────────────────────────────────────────────────────────────┘
                             │
                    ┌────────┴────────┐
                    │   Router Layer   │
                    │ (React Router)   │
                    └────────┬────────┘
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
    ┌─────▼─────┐      ┌─────▼─────┐    ┌─────▼─────┐
    │  Patient  │      │  Doctor   │    │   Admin   │
    │  Portal   │      │  Portal   │    │  Portal   │
    └─────┬─────┘      └─────┬─────┘    └─────┬─────┘
          │                  │                  │
          └──────────────────┼──────────────────┘
                             │
                    ┌────────▼────────┐
                    │  API Service    │
                    │   (Axios)       │
                    └────────┬────────┘
                             │
          ┌──────────────────┼──────────────────┐
          │                  │                  │
    ┌─────▼─────┐      ┌─────▼─────┐    ┌─────▼─────┐
    │  Doctor   │      │  Doctor   │    │Appointment│
    │Availability│      │Appointment│    │  Booking  │
    │    API    │      │Management │    │    API    │
    │  :5112    │      │    API    │    │  :5167    │
    │           │      │   :5113   │    │           │
    └───────────┘      └───────────┘    └───────────┘
```

## User Flow Diagrams

### Patient Journey
```
┌──────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────┐
│  Login   │───▶│Browse Slots  │───▶│Select Slot   │───▶│  Book    │
│          │    │              │    │              │    │          │
└──────────┘    └──────────────┘    └──────────────┘    └──────────┘
     │                                                          │
     │                                                          ▼
     │                                                   ┌──────────┐
     └──────────────────────────────────────────────────│Confirmed │
                                                        └──────────┘
```

### Doctor Journey
```
┌──────────┐    ┌──────────────┐    ┌──────────────┐
│  Login   │───▶│View All Apps │───▶│Filter by     │
│          │    │              │    │Status        │
└──────────┘    └──────────────┘    └──────┬───────┘
                                           │
                      ┌────────────────────┼────────────────────┐
                      │                    │                    │
                ┌─────▼─────┐        ┌─────▼─────┐      ┌─────▼─────┐
                │ Complete  │        │  Cancel   │      │  Refresh  │
                │Appointment│        │Appointment│      │   Data    │
                └───────────┘        └───────────┘      └───────────┘
```

### Admin Journey
```
┌──────────┐    ┌──────────────┐    ┌──────────────┐
│  Login   │───▶│View Dashboard│───▶│Create New    │
│          │    │& Statistics  │    │Slot          │
└──────────┘    └──────────────┘    └──────┬───────┘
                                           │
                                    ┌──────▼───────┐
                                    │ Enter Slot   │
                                    │ Details:     │
                                    │ - Doctor     │
                                    │ - Date/Time  │
                                    │ - Cost       │
                                    └──────┬───────┘
                                           │
                                    ┌──────▼───────┐
                                    │Save & Update │
                                    │ Dashboard    │
                                    └──────────────┘
```

## Component Hierarchy

```
App (Router + Auth Provider)
├── LoginPage
│   ├── RoleSelector
│   ├── EmailInput
│   └── PasswordInput
├── PatientDashboard
│   ├── Header (with logout)
│   ├── AvailableSlotsList
│   │   └── SlotCard[]
│   │       ├── DoctorInfo
│   │       ├── DateTimeDisplay
│   │       ├── CostDisplay
│   │       └── BookButton
│   └── BookingModal
│       ├── SlotSummary
│       ├── PatientNameInput
│       └── ConfirmationButtons
├── DoctorDashboard
│   ├── Header (with refresh & logout)
│   ├── StatisticsCards
│   │   ├── TotalCard
│   │   ├── ReservedCard
│   │   └── AvailableCard
│   ├── FilterTabs
│   │   ├── AllTab
│   │   ├── ReservedTab
│   │   └── AvailableTab
│   └── AppointmentsTable
│       └── AppointmentRow[]
│           ├── DateTimeCell
│           ├── DoctorCell
│           ├── CostCell
│           ├── StatusBadge
│           └── ActionButtons
└── AdminDashboard
    ├── Header (with add, refresh & logout)
    ├── StatisticsGrid
    │   ├── TotalSlotsCard
    │   ├── ReservedCard
    │   ├── AvailableCard
    │   └── RevenueCard
    ├── SlotsTable
    │   └── SlotRow[]
    │       ├── DateTimeCell
    │       ├── DoctorCell
    │       ├── CostCell
    │       └── StatusBadge
    └── AddSlotModal
        ├── DoctorNameInput
        ├── DateInput
        ├── TimeInput
        ├── CostInput
        └── SubmitButton
```

## State Management

```
┌─────────────────┐
│  AuthContext    │ (Global)
│  - user         │
│  - login()      │
│  - logout()     │
└─────────────────┘
         │
         ├────▶ PatientDashboard
         │      ├─ availableSlots (local)
         │      ├─ selectedSlot (local)
         │      └─ loading (local)
         │
         ├────▶ DoctorDashboard
         │      ├─ allSlots (local)
         │      ├─ filterStatus (local)
         │      └─ loading (local)
         │
         └────▶ AdminDashboard
                ├─ allSlots (local)
                ├─ newSlot (local)
                └─ loading (local)
```

## Data Flow

### Booking an Appointment (Patient)
```
User Action          Frontend                  API Service              Backend
    │                    │                          │                      │
    ├─[Select Slot]─────▶│                          │                      │
    │                    ├─[Show Modal]             │                      │
    │                    │                          │                      │
    ├─[Confirm]─────────▶│                          │                      │
    │                    ├─bookAppointment()───────▶│                      │
    │                    │                          ├─POST /api/...───────▶│
    │                    │                          │                      ├─[Save to DB]
    │                    │                          │◀────200 OK──────────┤
    │                    │◀─────Success────────────┤                      │
    │                    ├─[Close Modal]            │                      │
    │                    ├─loadAvailableSlots()────▶│                      │
    │                    │                          ├─GET /api/...────────▶│
    │                    │◀─────Slots[]────────────┤◀────Slots[]──────────┤
    │◀──[UI Updated]─────┤                          │                      │
```

### Managing Appointments (Doctor)
```
User Action          Frontend                  API Service              Backend
    │                    │                          │                      │
    ├─[Load Page]───────▶│                          │                      │
    │                    ├─loadAllSlots()──────────▶│                      │
    │                    │                          ├─GET /api/all───────▶│
    │                    │◀─────Slots[]────────────┤◀────Slots[]──────────┤
    │                    ├─[Display Table]          │                      │
    │                    │                          │                      │
    ├─[Complete]────────▶│                          │                      │
    │                    ├─completeAppointment()───▶│                      │
    │                    │                          ├─PUT /api/complete──▶│
    │                    │                          │                      ├─[Update DB]
    │                    │◀─────Success────────────┤◀────200 OK──────────┤
    │                    ├─[Refresh Data]           │                      │
    │◀──[UI Updated]─────┤                          │                      │
```

### Creating Slots (Admin)
```
User Action          Frontend                  API Service              Backend
    │                    │                          │                      │
    ├─[Add Slot]────────▶│                          │                      │
    │                    ├─[Show Modal]             │                      │
    │                    │                          │                      │
    ├─[Fill Form]───────▶│                          │                      │
    ├─[Submit]──────────▶│                          │                      │
    │                    ├─addSlot()───────────────▶│                      │
    │                    │                          ├─POST /api/slot─────▶│
    │                    │                          │                      ├─[Create in DB]
    │                    │◀─────Success────────────┤◀────201 Created─────┤
    │                    ├─[Close Modal]            │                      │
    │                    ├─loadAllSlots()──────────▶│                      │
    │                    │◀─────Slots[]────────────┤◀────Slots[]──────────┤
    │◀──[UI Updated]─────┤                          │                      │
```

## Technology Stack Details

```
┌─────────────────────────────────────────────────────┐
│                   Frontend Layer                     │
│  - React 19 (UI Library)                            │
│  - TypeScript (Type Safety)                         │
│  - React Router DOM v7 (Routing)                    │
│  - Vite (Build Tool)                                │
│  - CSS (Styling with Utilities)                     │
│  - Lucide React (Icons)                             │
└─────────────────────────────────────────────────────┘
                         │
┌─────────────────────────────────────────────────────┐
│                 Communication Layer                  │
│  - Axios (HTTP Client)                              │
│  - REST API                                         │
│  - JSON (Data Format)                               │
└─────────────────────────────────────────────────────┘
                         │
┌─────────────────────────────────────────────────────┐
│                   Backend Layer                      │
│  - ASP.NET Core Web API                             │
│  - PostgreSQL (Database)                            │
│  - Entity Framework Core (ORM)                      │
│  - CORS Enabled                                     │
└─────────────────────────────────────────────────────┘
```
