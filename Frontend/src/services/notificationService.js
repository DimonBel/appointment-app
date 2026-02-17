import { requestWithAuthRetry } from './httpClient'

const API_URL = import.meta.env.VITE_NOTIFICATION_API_URL || '/api/notification'

class NotificationService {
  async getNotifications(userId, token, page = 1, pageSize = 20) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/notifications?userId=${encodeURIComponent(userId)}&page=${page}&pageSize=${pageSize}`,
    }, token)
    return response.data
  }

  async getUnreadCount(userId, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/notifications/unread-count?userId=${encodeURIComponent(userId)}`,
    }, token)
    return response.data
  }

  async getUnreadNotifications(userId, token) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${API_URL}/notifications/unread?userId=${encodeURIComponent(userId)}`,
    }, token)
    return response.data
  }

  async markAsRead(notificationId, token) {
    const response = await requestWithAuthRetry({
      method: 'put',
      url: `${API_URL}/notifications/${notificationId}/read`,
    }, token)
    return response.data
  }

  async markAllAsRead(userId, token) {
    const response = await requestWithAuthRetry({
      method: 'put',
      url: `${API_URL}/notifications/read-all?userId=${encodeURIComponent(userId)}`,
    }, token)
    return response.data
  }

  async deleteNotification(notificationId, token) {
    const response = await requestWithAuthRetry({
      method: 'delete',
      url: `${API_URL}/notifications/${notificationId}`,
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
