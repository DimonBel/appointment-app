import type { Service, ApiEndpoint, Workflow } from '../types/api';

export const SERVICES: Service[] = [
  {
    name: 'DoctorAvailability',
    port: 5112,
    baseUrl: 'http://localhost:5112',
    description: 'Manages doctor time slots and availability',
    status: 'unknown'
  },
  {
    name: 'DoctorAppointmentManagement',
    port: 5113,
    baseUrl: 'http://localhost:5113',
    description: 'Handles appointment status management',
    status: 'unknown'
  },
  {
    name: 'AppointmentBooking',
    port: 5167,
    baseUrl: 'http://localhost:5167',
    description: 'Manages appointment booking process',
    status: 'unknown'
  }
];

export const API_ENDPOINTS: ApiEndpoint[] = [
  // DoctorAvailability endpoints
  {
    id: '1',
    method: 'GET',
    route: '/api/DoctorSlot/all',
    description: 'Get all doctor slots',
    service: 'DoctorAvailability',
    port: 5112,
    response: 'IQueryable<SlotModel>'
  },
  {
    id: '2',
    method: 'GET',
    route: '/api/DoctorSlot/available',
    description: 'Get available slots only',
    service: 'DoctorAvailability',
    port: 5112,
    response: 'IQueryable<SlotModel>'
  },
  {
    id: '3',
    method: 'POST',
    route: '/api/DoctorSlot',
    description: 'Add new doctor slot',
    service: 'DoctorAvailability',
    port: 5112,
    request: 'SlotModel'
  },

  // DoctorAppointmentManagement endpoints
  {
    id: '4',
    method: 'PUT',
    route: '/api/DoctorAppoinmentManagement/cancel',
    description: 'Cancel appointment',
    service: 'DoctorAppointmentManagement',
    port: 5113,
    parameters: [
      { name: 'SlotId', type: 'Guid', required: true }
    ]
  },
  {
    id: '5',
    method: 'PUT',
    route: '/api/DoctorAppoinmentManagement/complete',
    description: 'Mark appointment as complete',
    service: 'DoctorAppointmentManagement',
    port: 5113,
    parameters: [
      { name: 'SlotId', type: 'Guid', required: true }
    ]
  },

  // AppointmentBooking endpoints
  {
    id: '6',
    method: 'GET',
    route: '/api/ViewAvaliableSlot',
    description: 'Get available slots for booking',
    service: 'AppointmentBooking',
    port: 5167,
    response: 'List<SlotModel>'
  },
  {
    id: '7',
    method: 'PUT',
    route: '/api/ChangeAppoinmentStatus',
    description: 'Change appointment status',
    service: 'AppointmentBooking',
    port: 5167,
    request: { SlotId: 'Guid', StatusId: 'int' }
  },
  {
    id: '8',
    method: 'POST',
    route: '/api/AppoimentBooking',
    description: 'Book an appointment',
    service: 'AppointmentBooking',
    port: 5167,
    request: 'AppoimentBookingModel'
  }
];

export const WORKFLOWS: Workflow[] = [
  {
    id: 'patient-booking',
    name: 'Patient Appointment Booking',
    description: 'Complete flow for patients booking appointments',
    steps: [
      {
        id: 'view-slots',
        name: 'View Available Slots',
        description: 'Patient views available time slots',
        endpoint: '/api/ViewAvaliableSlot',
        method: 'GET',
        service: 'AppointmentBooking',
        position: { x: 100, y: 100 }
      },
      {
        id: 'book-appointment',
        name: 'Book Appointment',
        description: 'Patient selects and books a slot',
        endpoint: '/api/AppoimentBooking',
        method: 'POST',
        service: 'AppointmentBooking',
        position: { x: 300, y: 100 }
      },
      {
        id: 'notification',
        name: 'Receive Confirmation',
        description: 'Patient receives booking confirmation',
        endpoint: 'RabbitMQ',
        method: 'PUBLISH',
        service: 'MessageQueue',
        position: { x: 500, y: 100 }
      }
    ],
    connections: [
      { from: 'view-slots', to: 'book-appointment' },
      { from: 'book-appointment', to: 'notification' }
    ]
  },
  {
    id: 'doctor-management',
    name: 'Doctor Appointment Management',
    description: 'Flow for doctors managing their appointments',
    steps: [
      {
        id: 'view-all-slots',
        name: 'View All Slots',
        description: 'Doctor views all their slots',
        endpoint: '/api/DoctorSlot/all',
        method: 'GET',
        service: 'DoctorAvailability',
        position: { x: 100, y: 250 }
      },
      {
        id: 'complete-appointment',
        name: 'Complete Appointment',
        description: 'Mark appointment as completed',
        endpoint: '/api/DoctorAppoinmentManagement/complete',
        method: 'PUT',
        service: 'DoctorAppointmentManagement',
        position: { x: 300, y: 250 }
      },
      {
        id: 'cancel-appointment',
        name: 'Cancel Appointment',
        description: 'Cancel an appointment',
        endpoint: '/api/DoctorAppoinmentManagement/cancel',
        method: 'PUT',
        service: 'DoctorAppointmentManagement',
        position: { x: 300, y: 350 }
      }
    ],
    connections: [
      { from: 'view-all-slots', to: 'complete-appointment' },
      { from: 'view-all-slots', to: 'cancel-appointment' }
    ]
  },
  {
    id: 'admin-slot-management',
    name: 'Admin Slot Management',
    description: 'Administrative flow for managing doctor slots',
    steps: [
      {
        id: 'add-slot',
        name: 'Add New Slot',
        description: 'Admin adds new time slot',
        endpoint: '/api/DoctorSlot',
        method: 'POST',
        service: 'DoctorAvailability',
        position: { x: 100, y: 450 }
      },
      {
        id: 'view-available',
        name: 'Check Availability',
        description: 'View currently available slots',
        endpoint: '/api/DoctorSlot/available',
        method: 'GET',
        service: 'DoctorAvailability',
        position: { x: 300, y: 450 }
      }
    ],
    connections: [
      { from: 'add-slot', to: 'view-available' }
    ]
  }
];