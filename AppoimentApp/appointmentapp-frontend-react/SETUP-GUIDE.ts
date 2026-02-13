/**
 * APPOINTMENT APP - SETUP GUIDE
 * 
 * This file contains instructions for running the full appointment application.
 */

// WHAT WAS BUILT:
// 1. Complete Patient Portal - Book appointments, view available slots
// 2. Complete Doctor Portal - Manage appointments, complete/cancel
// 3. Complete Admin Portal - Create slots, manage doctors, track revenue
// 4. Authentication System - Role-based access control
// 5. Full API Integration - All three backend services connected

// AVAILABLE APPS:
// - App.tsx (NEW) - Full appointment system with routing and authentication
// - App-old-dashboard.tsx (OLD) - API testing dashboard

// TO RUN THE APPLICATION:
// 1. Ensure all backend services are running:
//    - DoctorAvailability API on http://localhost:5112
//    - DoctorAppointmentManagement API on http://localhost:5113
//    - AppointmentBooking API on http://localhost:5167

// 2. Install dependencies (if not already done):
//    npm install

// 3. Start the development server:
//    npm run dev

// 4. Open http://localhost:5173 in your browser

// LOGIN CREDENTIALS (Demo Mode):
// - Enter ANY email and password
// - Select your role:
//   * Patient - To book appointments
//   * Doctor - To manage appointments
//   * Admin - To create and manage slots

// USER FLOWS:

// ADMIN FLOW:
// 1. Login as Admin
// 2. Click "Add Slot" to create appointment slots
// 3. Fill in: Doctor Name, Date, Time, Cost
// 4. View all slots and revenue statistics

// PATIENT FLOW:
// 1. Login as Patient
// 2. Browse available appointment slots
// 3. Click "Book Appointment" on desired slot
// 4. Enter your name and confirm booking

// DOCTOR FLOW:
// 1. Login as Doctor
// 2. View all appointments (reserved & available)
// 3. Filter by status (All, Reserved, Available)
// 4. Mark reserved appointments as "Complete"
// 5. Cancel appointments if needed

// API ENDPOINTS USED:

// DoctorAvailability API:
// - GET /api/DoctorSlot/all - Get all slots
// - GET /api/DoctorSlot/available - Get available slots
// - POST /api/DoctorSlot - Add new slot

// DoctorAppointmentManagement API:
// - PUT /api/DoctorAppoinmentManagement/cancel?SlotId={id} - Cancel appointment
// - PUT /api/DoctorAppoinmentManagement/complete?SlotId={id} - Complete appointment

// AppointmentBooking API:
// - GET /api/ViewAvaliableSlot - Get available slots for booking
// - POST /api/AppoimentBooking - Book appointment
// - PUT /api/ChangeAppoinmentStatus?SlotId={id}&StatusId={status} - Change status

// SWITCHING BETWEEN APPS:
// To use the old API dashboard:
// 1. mv src/App.tsx src/App-full-system.tsx
// 2. mv src/App-old-dashboard.tsx src/App.tsx
// 3. Refresh browser

// To switch back:
// 1. mv src/App.tsx src/App-old-dashboard.tsx
// 2. mv src/App-full-system.tsx src/App.tsx
// 3. Refresh browser

export const APP_INFO = {
  version: '2.0.0',
  name: 'Healthcare Appointment System',
  description: 'Full-featured appointment management system',
  roles: ['Patient', 'Doctor', 'Admin'],
  features: [
    'Role-based authentication',
    'Patient appointment booking',
    'Doctor appointment management',
    'Admin slot creation and management',
    'Real-time data updates',
    'Revenue tracking',
    'Responsive design'
  ]
};
