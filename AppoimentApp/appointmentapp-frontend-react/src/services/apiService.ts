import axios from 'axios';
import type { Slot, AppointmentBooking } from '../types/api';

const API_BASE_URLS = {
  DoctorAvailability: 'http://localhost:5112/api',
  DoctorAppointmentManagement: 'http://localhost:5113/api',
  AppointmentBooking: 'http://localhost:5167/api'
};

class ApiService {
  // Health check for services
  async checkServiceHealth(serviceName: keyof typeof API_BASE_URLS): Promise<boolean> {
    try {
      const baseUrl = API_BASE_URLS[serviceName];
      // Try to access the swagger endpoint or a basic health check
      await axios.get(`${baseUrl.replace('/api', '')}/swagger`, { timeout: 5000 });
      return true;
    } catch (error) {
      return false;
    }
  }

  // DoctorAvailability API
  async getAllSlots(): Promise<Slot[]> {
    const response = await axios.get(`${API_BASE_URLS.DoctorAvailability}/DoctorSlot/all`);
    return response.data;
  }

  async getAvailableSlots(): Promise<Slot[]> {
    const response = await axios.get(`${API_BASE_URLS.DoctorAvailability}/DoctorSlot/available`);
    return response.data;
  }

  async addSlot(slot: Omit<Slot, 'id'>): Promise<void> {
    await axios.post(`${API_BASE_URLS.DoctorAvailability}/DoctorSlot`, slot);
  }

  // DoctorAppointmentManagement API
  async cancelAppointment(slotId: string): Promise<void> {
    await axios.put(`${API_BASE_URLS.DoctorAppointmentManagement}/DoctorAppoinmentManagement/cancel`, null, {
      params: { SlotId: slotId }
    });
  }

  async completeAppointment(slotId: string): Promise<void> {
    await axios.put(`${API_BASE_URLS.DoctorAppointmentManagement}/DoctorAppoinmentManagement/complete`, null, {
      params: { SlotId: slotId }
    });
  }

  // AppointmentBooking API
  async getAvailableSlotsForBooking(): Promise<Slot[]> {
    const response = await axios.get(`${API_BASE_URLS.AppointmentBooking}/ViewAvaliableSlot`);
    return response.data;
  }

  async changeAppointmentStatus(slotId: string, statusId: number): Promise<void> {
    await axios.put(`${API_BASE_URLS.AppointmentBooking}/ChangeAppoinmentStatus`, null, {
      params: { SlotId: slotId, StatusId: statusId }
    });
  }

  async bookAppointment(booking: Partial<AppointmentBooking>): Promise<void> {
    const bookingData = {
      ReservedAt: booking.reservedAt || new Date().toISOString(),
      SlotId: booking.slotId,
      PatientId: booking.patientId,
      PatientName: booking.patientName,
      DoctorName: booking.doctorName
    };
    await axios.post(`${API_BASE_URLS.AppointmentBooking}/AppoimentBooking`, bookingData);
  }

  // Test endpoint connectivity
  async testEndpoint(service: keyof typeof API_BASE_URLS, endpoint: string, method: string = 'GET'): Promise<{ success: boolean; data?: any; error?: string }> {
    try {
      const url = `${API_BASE_URLS[service]}${endpoint}`;
      let response;

      switch (method.toUpperCase()) {
        case 'GET':
          response = await axios.get(url);
          break;
        case 'POST':
          response = await axios.post(url, {});
          break;
        case 'PUT':
          response = await axios.put(url, {});
          break;
        default:
          response = await axios.get(url);
      }

      return { success: true, data: response.data };
    } catch (error: any) {
      return { 
        success: false, 
        error: error.response?.data?.message || error.message || 'Unknown error' 
      };
    }
  }
}

export default new ApiService();