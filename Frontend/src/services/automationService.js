import { requestWithAuthRetry } from './httpClient'

const AUTOMATION_BASE_URL = import.meta.env.VITE_AUTOMATION_API_URL || '/api/automation'

export const automationService = {
  // Start a new conversation with AI
  async startConversation() {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${AUTOMATION_BASE_URL}/conversations/start`
    })
    return response.data
  },

  // Get active conversation for current user
  async getActiveConversation() {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${AUTOMATION_BASE_URL}/conversations/active`
    })
    return response.data
  },

  // Get conversation messages
  async getConversationMessages(conversationId) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${AUTOMATION_BASE_URL}/conversations/${conversationId}/messages`
    })
    return response.data
  },

  // Send a message to the AI
  async sendMessage(message, conversationId = null, selectedOption = null) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${AUTOMATION_BASE_URL}/conversations/send`,
      data: {
        message,
        conversationId,
        selectedOption
      }
    })
    return response.data
  },

  // Get booking draft
  async getBookingDraft(conversationId) {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${AUTOMATION_BASE_URL}/booking/draft/${conversationId}`
    })
    return response.data
  },

  // Submit booking
  async submitBooking(conversationId) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${AUTOMATION_BASE_URL}/booking/submit`,
      data: { conversationId }
    })
    return response.data
  },

  // Cancel booking draft
  async cancelBookingDraft(draftId) {
    const response = await requestWithAuthRetry({
      method: 'post',
      url: `${AUTOMATION_BASE_URL}/booking/cancel/${draftId}`
    })
    return response.data
  },

  // Get quick booking options
  async getBookingOptions() {
    const response = await requestWithAuthRetry({
      method: 'get',
      url: `${AUTOMATION_BASE_URL}/options`
    })
    return response.data
  }
}