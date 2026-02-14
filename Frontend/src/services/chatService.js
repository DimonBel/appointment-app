import axios from 'axios'

const API_URL = import.meta.env.VITE_CHAT_API_URL || '/api/chat'

class ChatService {
  // Get all users (for chat list)
  async getChats(token) {
    const response = await axios.get(`${API_URL}/users`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Get messages with a specific user
  async getMessages(userId, token) {
    const response = await axios.get(`${API_URL}/messages/${userId}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Send message to a user
  async sendMessage(receiverId, message, token) {
    const response = await axios.post(
      `${API_URL}/messages`,
      { receiverId, content: message },
      {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      }
    )
    return response.data
  }

  // Get recent messages
  async getRecentMessages(token, count = 20) {
    const response = await axios.get(`${API_URL}/messages/recent?count=${count}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Search users
  async searchUsers(query, token) {
    const response = await axios.get(`${API_URL}/users/search?query=${encodeURIComponent(query)}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Get user by ID
  async getUser(userId, token) {
    const response = await axios.get(`${API_URL}/users/${userId}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }
}

export const chatService = new ChatService()
