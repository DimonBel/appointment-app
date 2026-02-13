# Appointment App Frontend

A comprehensive healthcare appointment management system built with React, TypeScript, and Vite.

## Features

### Patient Portal
- **View Available Appointments**: Browse all available time slots with doctors
- **Book Appointments**: Select and book appointments with healthcare professionals
- **Real-time Updates**: See appointment availability in real-time
- **User-Friendly Interface**: Clean, modern UI for easy navigation

### Doctor Portal
- **Manage Appointments**: View all appointments (reserved and available)
- **Complete Appointments**: Mark appointments as completed
- **Cancel Appointments**: Cancel appointments when necessary
- **Dashboard Analytics**: View statistics on total, reserved, and available appointments
- **Filter Options**: Filter appointments by status (all, reserved, available)

### Admin Portal
- **Slot Management**: Create new appointment slots
- **Doctor Management**: Assign doctors to time slots
- **Revenue Tracking**: Monitor revenue from booked appointments
- **Comprehensive Dashboard**: View all slots and their statuses
- **Analytics**: Track total slots, reserved, available, and revenue metrics

## Tech Stack

- **Frontend Framework**: React 19
- **Language**: TypeScript
- **Build Tool**: Vite
- **Routing**: React Router DOM v7
- **HTTP Client**: Axios
- **Icons**: Lucide React
- **Styling**: CSS (with modern design system)

## Backend Integration

The application connects to three microservices:

1. **Doctor Availability API** (Port: 5112)
   - Get all slots
   - Get available slots
   - Add new slots

2. **Doctor Appointment Management API** (Port: 5113)
   - Cancel appointments
   - Complete appointments

3. **Appointment Booking API** (Port: 5167)
   - View available slots
   - Book appointments
   - Change appointment status

## Getting Started

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

### Environment Configuration

The API endpoints are configured in `src/services/apiService.ts`:

```typescript
const API_BASE_URLS = {
  DoctorAvailability: 'http://localhost:5112/api',
  DoctorAppointmentManagement: 'http://localhost:5113/api',
  AppointmentBooking: 'http://localhost:5167/api'
};
```

## Project Structure

```
src/
├── components/
│   └── shared/
│       └── UIComponents.tsx      # Reusable UI components
├── contexts/
│   └── AuthContext.tsx           # Authentication context
├── pages/
│   ├── LoginPage.tsx             # Login page
│   ├── PatientDashboard.tsx      # Patient portal
│   ├── DoctorDashboard.tsx       # Doctor portal
│   └── AdminDashboard.tsx        # Admin portal
├── services/
│   └── apiService.ts             # API service layer
├── types/
│   └── api.ts                    # TypeScript type definitions
├── App-new.tsx                   # Main app with routing
└── main.tsx                      # Application entry point
```

## User Roles

### Patient
- Book appointments
- View available time slots
- See doctor information and costs

### Doctor
- View all appointments
- Complete appointments
- Cancel appointments
- Filter by appointment status

### Admin
- Create appointment slots
- Assign doctors
- Set pricing
- View revenue analytics
- Manage overall system

## Authentication

The application uses a mock authentication system. In production, this should be replaced with a proper authentication service (JWT, OAuth, etc.).

**Demo Login:**
- Enter any email and password
- Select your role (Patient, Doctor, or Admin)
- You'll be redirected to the appropriate dashboard

## API Integration

All API calls are centralized in `apiService.ts`:

```typescript
// Example: Book an appointment
await apiService.bookAppointment({
  slotId: 'slot-id',
  patientId: 'patient-id',
  patientName: 'John Doe',
  doctorName: 'Dr. Smith'
});

// Example: Get available slots
const slots = await apiService.getAvailableSlots();

// Example: Complete appointment
await apiService.completeAppointment('slot-id');
```

## Features in Detail

### Real-time Data
- Automatic refresh of appointment data
- Loading states for better UX
- Error handling for failed API calls

### Responsive Design
- Mobile-friendly interface
- Adaptive layouts for different screen sizes
- Touch-friendly controls

### Modern UI
- Gradient backgrounds
- Smooth transitions
- Icon-based navigation
- Status badges
- Modal dialogs

## Development

```bash
# Run linter
npm run lint

# Type checking
npm run tsc

# Development with hot reload
npm run dev
```

## Building for Production

```bash
# Build optimized production bundle
npm run build

# Preview production build locally
npm run preview
```

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please open an issue in the repository.
