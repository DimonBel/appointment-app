# Appointment API Dashboard

A comprehensive React-based dashboard that visualizes and manages the ASP.NET Appointment Booking microservices API.

## Features

### 🔍 Service Status Monitoring
- Real-time health check for all microservices
- Visual status indicators (online/offline)
- Quick access to Swagger documentation
- Service port and endpoint information

### 📡 API Endpoints Explorer
- Complete catalog of all API endpoints
- Organized by microservice
- HTTP method and response type indicators
- Interactive endpoint testing
- Direct links to Swagger documentation

### 🔄 Workflow Visualization
- Visual representation of API workflows
- Patient booking flow
- Doctor management flow
- Admin slot management flow
- Interactive connection diagrams

### 🧪 Interactive API Testing
- Test any endpoint directly from the dashboard
- Request/response visualization
- Parameter configuration
- Error handling and display

### 🗄️ Data Model Documentation
- Entity relationship diagrams
- Database configuration details
- Status code explanations
- Model variations across services

## Microservices Architecture

The dashboard monitors three ASP.NET Core microservices:

### 1. DoctorAvailability (Port: 5112)
- **Purpose**: Manages doctor time slots and availability
- **Endpoints**:
  - `GET /api/DoctorSlot/all` - Get all slots
  - `GET /api/DoctorSlot/available` - Get available slots
  - `POST /api/DoctorSlot` - Add new slot
- **Database**: `doctoravailabilitydb_dev`

### 2. DoctorAppointmentManagement (Port: 5113)
- **Purpose**: Handles appointment status management
- **Endpoints**:
  - `PUT /api/DoctorAppoinmentManagement/cancel` - Cancel appointment
  - `PUT /api/DoctorAppoinmentManagement/complete` - Complete appointment
- **Database**: `doctorappointmentmanagementdb_dev`

### 3. AppointmentBooking (Port: 5167)
- **Purpose**: Manages appointment booking process
- **Endpoints**:
  - `GET /api/ViewAvaliableSlot` - View available slots
  - `POST /api/AppoimentBooking` - Book appointment
  - `PUT /api/ChangeAppoinmentStatus` - Change appointment status
- **Database**: `appointmentbookingdb_dev`

## Getting Started

### Prerequisites
- Node.js 18+ and npm
- PostgreSQL running on localhost:5432
- RabbitMQ running on localhost
- All ASP.NET microservices running

### Installation

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Start the development server**:
   ```bash
   npm run dev
   ```

3. **Access the dashboard**:
   Open `http://localhost:5174` in your browser

### Building for Production

```bash
npm run build
```

### Linting

```bash
npm run lint
```

## Access URLs

- **Frontend Dashboard**: `http://localhost:5174`
- **DoctorAvailability Swagger**: `http://localhost:5112/swagger`
- **AppointmentManagement Swagger**: `http://localhost:5113/swagger`
- **AppointmentBooking Swagger**: `http://localhost:5167/swagger`

## Technology Stack

### Frontend
- **React 19** - Modern React with latest features
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool and dev server
- **Tailwind CSS 4** - Utility-first CSS framework with custom components
- **Lucide React** - Beautiful modern icons
- **Axios** - HTTP client for API calls
- **React Router** - Client-side routing
- **Recharts** - Charting library

### Styling & Design
- **Modern Design System** - Complete component library
- **Tailwind CSS** - Utility-first with custom @layer components
- **Glass Morphism** - Modern glassmorphic effects
- **Custom Animations** - Smooth transitions and animations
- **Responsive Design** - Mobile-first approach
- **Inter Font** - Google Fonts integration

📖 **See [DESIGN_SYSTEM.md](./DESIGN_SYSTEM.md) for complete styling guide and component documentation**

### Backend (ASP.NET Core Microservices)
- **ASP.NET Core** - Web framework
- **PostgreSQL** - Database
- **Entity Framework Core** - ORM
- **RabbitMQ** - Message queue
- **Swagger/OpenAPI** - API documentation

## Development Notes

This dashboard provides a complete overview of the appointment booking API structure, workflows, and data models. It serves as both documentation and an interactive testing tool for developers working with the microservices architecture.

## 🎨 Styling Features

### Modern UI Components
- **Pre-built Component Classes**: `.btn-primary`, `.card`, `.badge-success`, etc.
- **Tailwind Utilities**: Full Tailwind CSS library available
- **Custom Animations**: Fade, slide, scale animations
- **Glass Morphism**: Beautiful translucent effects
- **Gradient Text**: Eye-catching text gradients
- **Responsive Grid**: Mobile-first responsive design

### Quick Style Examples

**Button:**
```jsx
<button className="btn-primary">
  <Icon className="w-5 h-5" />
  <span>Click Me</span>
</button>
```

**Card:**
```jsx
<div className="card-interactive">
  <h3 className="text-lg font-semibold">Card Title</h3>
  <p className="text-gray-600">Card content</p>
</div>
```

**Badge:**
```jsx
<span className="badge-success">Available</span>
```

For more examples and complete documentation, see **[DESIGN_SYSTEM.md](./DESIGN_SYSTEM.md)**
