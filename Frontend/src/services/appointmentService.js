import axios from 'axios'
import { requestWithAuthRetry } from './httpClient'

const API_URL = import.meta.env.VITE_APPOINTMENT_API_URL || '/api/appointment'
const PROFESSIONALS_API_URL = '/api/professionals'

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

  async getAllOrdersForManagement(token, status = null, page = 1, pageSize = 100, sortBy = null, descending = false) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/orders/all`,
        params: {
          ...(status !== null ? { status } : {}),
          page,
          pageSize,
          ...(sortBy ? { sortBy } : {}),
          descending,
        },
      },
      token
    )
    return response.data
  }

  async getOrdersByClient(clientId, token, status = null, page = 1, pageSize = 100, professionalId = null) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/orders/client/${clientId}`,
        params: {
          ...(status !== null ? { status } : {}),
          page,
          pageSize,
          ...(professionalId ? { professionalId } : {}),
        },
      },
      token
    )
    return response.data
  }

  async getClientsByProfessional(professionalId, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/orders/professional/${professionalId}/clients`,
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
          url: `${PROFESSIONALS_API_URL}/user/${userId}`,
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
        url: `${PROFESSIONALS_API_URL}`,
        data: professionalData,
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
    const data = reason ? { reason } : {}
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/approve`,
        data,
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

  async generateBookingDocument(orderId, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/orders/${orderId}/booking-document/generate`,
        data: {},
      },
      token
    )
    return response.data
  }

  async getProfessionals(token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${PROFESSIONALS_API_URL}`,
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

  async getMonthlyAvailabilityStatus(professionalId, year, month, token) {
    const availabilityStatus = {}
    const daysInMonth = new Date(year, month, 0).getDate()

    // Create array of dates to check
    const dates = Array.from({ length: daysInMonth }, (_, i) => {
      const day = i + 1
      return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    })

    console.log('Fetching availability for dates:', dates)

    // Make parallel requests in batches of 5 to avoid overwhelming the server
    const batchSize = 5
    for (let i = 0; i < dates.length; i += batchSize) {
      const batch = dates.slice(i, i + batchSize)
      const promises = batch.map(async (date) => {
        try {
          const slots = await this.getAvailabilitySlots(professionalId, date, token)
          const slotsArray = Array.isArray(slots) ? slots : []
          
          const result = {
            date,
            hasSlots: slotsArray.length > 0,
            hasAvailableSlots: slotsArray.some(slot => slot.isAvailable),
            totalSlots: slotsArray.length,
            availableSlots: slotsArray.filter(s => s.isAvailable).length
          }
          
          console.log(`Date ${date}:`, result)
          return result
        } catch (error) {
          console.error(`Error fetching slots for ${date}:`, error)
          // If error fetching slots, mark as unavailable
          return {
            date,
            hasSlots: false,
            hasAvailableSlots: false,
            totalSlots: 0,
            availableSlots: 0
          }
        }
      })

      const results = await Promise.all(promises)
      results.forEach(result => {
        availabilityStatus[result.date] = result
      })
    }

    console.log('Final availability status:', availabilityStatus)
    return availabilityStatus
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

  async getAllAvailabilities(token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/availabilities/all`,
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
