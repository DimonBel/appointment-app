// API Service Layer
const API_BASE_URL = '/api'

// Helper function to handle API responses
const handleResponse = async (response) => {
  if (!response.ok) {
    const error = await response.text()
    throw new Error(error || `HTTP error! status: ${response.status}`)
  }
  
  const text = await response.text()
  return text ? JSON.parse(text) : null
}

// Generic API call function
const apiCall = async (endpoint, options = {}) => {
  const config = {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, config)
  return handleResponse(response)
}

// Orders API
export const ordersAPI = {
  // Get all orders for a client
  getByClient: async (clientId, status = null, page = 1, pageSize = 20) => {
    const params = new URLSearchParams({ page, pageSize })
    if (status) params.append('status', status)
    return apiCall(`/orders/client/${clientId}?${params}`)
  },

  // Get all orders for a professional
  getByProfessional: async (professionalId, status = null, page = 1, pageSize = 20) => {
    const params = new URLSearchParams({ page, pageSize })
    if (status) params.append('status', status)
    return apiCall(`/orders/professional/${professionalId}?${params}`)
  },

  // Get order by ID
  getById: async (id) => {
    return apiCall(`/orders/${id}`)
  },

  // Create new order
  create: async (orderData) => {
    return apiCall('/orders', {
      method: 'POST',
      body: JSON.stringify(orderData),
    })
  },

  // Update order
  update: async (id, orderData) => {
    return apiCall(`/orders/${id}`, {
      method: 'PUT',
      body: JSON.stringify(orderData),
    })
  },

  // Cancel order
  cancel: async (id) => {
    return apiCall(`/orders/${id}/cancel`, {
      method: 'POST',
    })
  },

  // Reschedule order
  reschedule: async (id, rescheduleData) => {
    return apiCall(`/orders/${id}/reschedule`, {
      method: 'POST',
      body: JSON.stringify(rescheduleData),
    })
  },

  // Complete order
  complete: async (id, notes = null) => {
    return apiCall(`/orders/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    })
  },

  // Approve order (for professionals)
  approve: async (id, approvalData) => {
    return apiCall(`/orders/${id}/approve`, {
      method: 'POST',
      body: JSON.stringify(approvalData),
    })
  },

  // Decline order (for professionals)
  decline: async (id, declineData) => {
    return apiCall(`/orders/${id}/decline`, {
      method: 'POST',
      body: JSON.stringify(declineData),
    })
  },

  // Complete order
  complete: async (id) => {
    return apiCall(`/orders/${id}/complete`, {
      method: 'POST',
    })
  },
}

// Professionals API
export const professionalsAPI = {
  // Get all professionals
  getAll: async (onlyAvailable = true, page = 1, pageSize = 20) => {
    const params = new URLSearchParams({ onlyAvailable, page, pageSize })
    return apiCall(`/professionals?${params}`)
  },

  // Get professional by ID
  getById: async (id) => {
    return apiCall(`/professionals/${id}`)
  },

  // Get professional by user ID
  getByUserId: async (userId) => {
    return apiCall(`/professionals/user/${userId}`)
  },

  // Create professional
  create: async (professionalData) => {
    return apiCall('/professionals', {
      method: 'POST',
      body: JSON.stringify(professionalData),
    })
  },

  // Update professional
  update: async (id, professionalData) => {
    return apiCall(`/professionals/${id}`, {
      method: 'PUT',
      body: JSON.stringify(professionalData),
    })
  },

  // Update availability
  updateAvailability: async (id, isAvailable) => {
    return apiCall(`/professionals/${id}/availability`, {
      method: 'PATCH',
      body: JSON.stringify({ isAvailable }),
    })
  },

  // Delete professional
  delete: async (id) => {
    return apiCall(`/professionals/${id}`, {
      method: 'DELETE',
    })
  },
}

// Availability API
export const availabilityAPI = {
  // Get availability for a professional
  getByProfessional: async (professionalId, startDate, endDate) => {
    const params = new URLSearchParams({ startDate, endDate })
    return apiCall(`/availability/professional/${professionalId}?${params}`)
  },

  // Get available slots
  getSlots: async (professionalId, date) => {
    return apiCall(`/availability/professional/${professionalId}/slots?date=${date}`)
  },

  // Create availability
  create: async (availabilityData) => {
    return apiCall('/availability', {
      method: 'POST',
      body: JSON.stringify(availabilityData),
    })
  },

  // Update availability
  update: async (id, availabilityData) => {
    return apiCall(`/availability/${id}`, {
      method: 'PUT',
      body: JSON.stringify(availabilityData),
    })
  },

  // Delete availability
  delete: async (id) => {
    return apiCall(`/availability/${id}`, {
      method: 'DELETE',
    })
  },
}

// Domain Configuration API
export const domainConfigAPI = {
  // Get all domain configurations
  getAll: async (onlyActive = true) => {
    const params = new URLSearchParams({ onlyActive })
    return apiCall(`/domain-configurations?${params}`)
  },

  // Get domain configuration by ID
  getById: async (id) => {
    return apiCall(`/domain-configurations/${id}`)
  },

  // Create domain configuration
  create: async (configData) => {
    return apiCall('/domain-configurations', {
      method: 'POST',
      body: JSON.stringify(configData),
    })
  },

  // Update domain configuration
  update: async (id, configData) => {
    return apiCall(`/domain-configurations/${id}`, {
      method: 'PUT',
      body: JSON.stringify(configData),
    })
  },
}

export default {
  orders: ordersAPI,
  professionals: professionalsAPI,
  availability: availabilityAPI,
  domainConfig: domainConfigAPI,
}
