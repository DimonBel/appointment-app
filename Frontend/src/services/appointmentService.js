import axios from 'axios'
import { requestWithAuthRetry } from './httpClient'

const API_URL = import.meta.env.VITE_APPOINTMENT_API_URL || '/api/appointment'

class AppointmentService {
  async getOrders(token, status = null) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/orders`,
        params: status !== null ? { status } : undefined,
      },
      token
    )
    return response.data
  }

  async getOrdersByClient(clientId, token, status = null, page = 1, pageSize = 100) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/orders/client/${clientId}`,
        params: {
          ...(status !== null ? { status } : {}),
          page,
          pageSize,
        },
      },
      token
    )
    return response.data
  }

  async createOrder(orderData, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders`,
        data: orderData,
      },
      token
    )
    return response.data
  }

  async getProfessionalByUserId(userId, token) {
    try {
      const response = await requestWithAuthRetry(
        {
          method: 'get',
          url: `${API_URL}/professionals/user/${userId}`,
        },
        token
      )
      return response.data
    } catch (error) {
      if (error.response?.status === 404) {
        return null
      }
      throw error
    }
  }

  async createProfessional(professionalData, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/professionals`,
        data: professionalData,
      },
      token
    )
    return response.data
  }

  async updateOrder(orderId, orderData, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'put',
        url: `${API_URL}/orders/${orderId}`,
        data: orderData,
      },
      token
    )
    return response.data
  }

  async cancelOrder(orderId, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/cancel`,
        data: {},
      },
      token
    )
    return response.data
  }

  async rescheduleOrder(orderId, newScheduledDateTime, notes, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/reschedule`,
        data: {
          newScheduledDateTime,
          notes,
        },
      },
      token
    )
    return response.data
  }

  async completeOrder(orderId, notes, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/complete`,
        data: {
          notes,
        },
      },
      token
    )
    return response.data
  }

  async approveOrder(orderId, reason, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/approve`,
        data: {
          reason: reason || null,
        },
      },
      token
    )
    return response.data
  }

  async declineOrder(orderId, reason, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/decline`,
        data: {
          reason: reason || 'Declined by doctor',
        },
      },
      token
    )
    return response.data
  }

  async getProfessionals(token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/professionals`,
      },
      token
    )
    return response.data
  }

  async getAvailability(professionalId, date, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/availabilities/slots/${professionalId}`,
        params: { date },
      },
      token
    )
    return response.data
  }

  async getAvailabilitySlots(professionalId, date, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/availabilities/slots/status/${professionalId}`,
        params: { date },
      },
      token
    )
    return response.data
  }

  async getAvailabilitiesByProfessional(professionalId, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/availabilities/professional/${professionalId}`,
      },
      token
    )
    return response.data
  }

  async createAvailability(availabilityData, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/availabilities`,
        data: availabilityData,
      },
      token
    )
    return response.data
  }

  async deleteAvailability(availabilityId, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'delete',
        url: `${API_URL}/availabilities/${availabilityId}`,
      },
      token
    )
    return response.data
  }
}

export const appointmentService = new AppointmentService()
