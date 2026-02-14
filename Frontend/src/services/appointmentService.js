import axios from 'axios'

const API_URL = import.meta.env.VITE_APPOINTMENT_API_URL || '/api/appointment'

class AppointmentService {
  async getOrders(token) {
    const response = await axios.get(`${API_URL}/orders`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  async createOrder(orderData, token) {
    const response = await axios.post(`${API_URL}/orders`, orderData, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  async updateOrder(orderId, orderData, token) {
    const response = await axios.put(`${API_URL}/orders/${orderId}`, orderData, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  async cancelOrder(orderId, token) {
    const response = await axios.delete(`${API_URL}/orders/${orderId}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  async getProfessionals(token) {
    const response = await axios.get(`${API_URL}/professionals`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  async getAvailability(professionalId, date, token) {
    const response = await axios.get(
      `${API_URL}/availability/${professionalId}`,
      {
        params: { date },
        headers: {
          Authorization: `Bearer ${token}`,
        },
      }
    )
    return response.data
  }
}

export const appointmentService = new AppointmentService()
