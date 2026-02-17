import axios from 'axios'
import { requestWithAuthRetry } from './httpClient'

const API_URL = import.meta.env.VITE_CHAT_API_URL || '/api/chat'

const getIdentityApiBase = () => {
  const configured = import.meta.env.VITE_IDENTITY_API_URL
  if (!configured) return '/api'
  return configured.endsWith('/auth') ? configured.replace(/\/auth$/, '') : configured
}

class ChatService {
  // Get all users (for chat list)
  async getChats(token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/users`,
      },
      token
    )
    return response.data
  }

  // Get messages with a specific user
  async getMessages(userId, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/messages/${userId}`,
      },
      token
    )
    return response.data
  }

  // Send message to a user
  async sendMessage(receiverId, message, token) {
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${API_URL}/messages`,
        data: { receiverId, content: message },
      },
      token
    )
    return response.data
  }

  // Get recent messages
  async getRecentMessages(token, count = 20) {
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${API_URL}/messages/recent?count=${count}`,
      },
      token
    )
    return response.data
  }

  // Search users via Chat API (supports role-agnostic doctor/client discovery)
  async searchUsers(query, token) {
    const encoded = encodeURIComponent(query)
    const identityApiBase = getIdentityApiBase()

    try {
      const identityResponse = await requestWithAuthRetry(
        {
          method: 'get',
          url: `${identityApiBase}/users/search?query=${encoded}`,
        },
        token
      )
      return identityResponse.data
    } catch {
      const chatResponse = await requestWithAuthRetry(
        {
          method: 'get',
          url: `${API_URL}/users/search?query=${encoded}`,
        },
        token
      )
      return chatResponse.data
    }
  }

  // Get user by ID - use Identity API
  async getUser(userId, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${identityApiBase}/users/${userId}`,
      },
      token
    )
    return response.data
  }
}

export const chatService = new ChatService()
