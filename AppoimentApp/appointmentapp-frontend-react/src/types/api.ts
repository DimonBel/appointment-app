export interface Slot {
  id: string;
  date: string;
  doctorId: string;
  doctorName: string;
  cost: number;
  isReserved?: boolean;
}

export interface AppointmentBooking {
  id: string;
  reservedAt: string;
  slotId: string;
  patientId: string;
  patientName: string;
  doctorName?: string;
  appointmentStatus?: number; // 1 = complete, 2 = cancel
}

export interface ApiEndpoint {
  id: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  route: string;
  description: string;
  service: string;
  port: number;
  request?: any;
  response?: any;
  parameters?: { name: string; type: string; required: boolean }[];
}

export interface Service {
  name: string;
  port: number;
  baseUrl: string;
  description: string;
  status: 'online' | 'offline' | 'unknown';
}

export interface WorkflowStep {
  id: string;
  name: string;
  description: string;
  endpoint: string;
  method: string;
  service: string;
  position: { x: number; y: number };
}

export interface Workflow {
  id: string;
  name: string;
  description: string;
  steps: WorkflowStep[];
  connections: { from: string; to: string }[];
}