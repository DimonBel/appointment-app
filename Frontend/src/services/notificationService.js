import { requestWithAuthRetry } from './httpClient'

const API_URL = import.meta.env.VITE_NOTIFICATION_API_URL || '/api/notifications'

// Request deduplication cache
const pendingRequests = new Map()

// Helper to deduplicate simultaneous requests
const deduplicateRequest = async (key, requestFn) => {
  if (pendingRequests.has(key)) {
    return pendingRequests.get(key)
  }

  const promise = requestFn()
    .finally(() => {
      pendingRequests.delete(key)
    })
  
  pendingRequests.set(key, promise)
  return promise
}

class NotificationService {
  async getNotifications(userId, token, page = 1, pageSize = 50) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}?userId=${encodeURIComponent(userId)}&page=${page}&pageSize=${pageSize}`,
    }, token)
    return response.data
  }

  async getNotificationsWithCount(userId, token, page = 1, pageSize = 50) {
    const cacheKey = `notifications-${userId}-${page}-${pageSize}`
    
    return deduplicateRequest(cacheKey, async () => {
      const response = await requestWithAuthRetry({
        method: 'get',
        url: `${API_URL}/with-count?userId=${encodeURIComponent(userId)}&page=${page}&pageSize=${pageSize}`,
      }, token)
      return response.data
    })
  }

  async getUnreadCount(userId, token) {
    const cacheKey = `unread-count-${userId}`
    
    return deduplicateRequest(cacheKey, async () => {
      const response = await requestWithAuthRetry({
        method: 'get',
        url: `${API_URL}/unread-count?userId=${encodeURIComponent(userId)}`,
      }, token)
      return response.data
    })
  }

  async getUnreadNotifications(userId, token, page = 1, pageSize = 50) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/unread?userId=${encodeURIComponent(userId)}&page=${page}&pageSize=${pageSize}`,
    }, token)
    return response.data
  }

  async markAsRead(notificationId, token) {
    const response = await requestWithAuthRetry({
      method: 'put',
      url: `${API_URL}/${notificationId}/read`,
    }, token)
    return response.data
  }

  async markAllAsRead(userId, token) {
    const response = await requestWithAuthRetry({
      method: 'put',
      url: `${API_URL}/read-all?userId=${encodeURIComponent(userId)}`,
    }, token)
    return response.data
  }

  async deleteNotification(notificationId, token) {
    const response = await requestWithAuthRetry({
      method: 'delete',
      url: `${API_URL}/${notificationId}`,
    }, token)
    return response.data
  }

  // Preferences
  async getPreferences(token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/preferences`,
    }, token)
    return response.data
  }

  async updatePreference(preferenceData, token) {
    const response = await requestWithAuthRetry({
      method: 'put',
      url: `${API_URL}/preferences`,
      data: preferenceData,
    }, token)
    return response.data
  }

  // Events - send events from other services
  async sendEvent(eventData, token) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${API_URL}/events`,
      data: eventData,
    }, token)
    return response.data
  }
}

export const notificationService = new NotificationService()
