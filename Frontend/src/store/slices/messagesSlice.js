import { createSlice } from '@reduxjs/toolkit'

const initialState = {
  messagesByChatId: {},
  isLoading: false,
  isTyping: false,
  typingUsers: [],
}

const messagesSlice = createSlice({
  name: 'messages',
  initialState,
  reducers: {
    setMessages: (state, action) => {
      const { chatId, messages } = action.payload
      state.messagesByChatId[chatId] = messages
    },
    addMessage: (state, action) => {
      const message = action.payload
      const chatId = message.chatId
      
      if (!state.messagesByChatId[chatId]) {
        state.messagesByChatId[chatId] = []
      }
      
      const exists = state.messagesByChatId[chatId].find(m => m.id === message.id)
      if (!exists) {
        state.messagesByChatId[chatId].push(message)
      }
    },
    updateMessage: (state, action) => {
      const { chatId, messageId, updates } = action.payload
      const messages = state.messagesByChatId[chatId]
      if (messages) {
        const index = messages.findIndex(m => m.id === messageId)
        if (index !== -1) {
          messages[index] = { ...messages[index], ...updates }
        }
      }
    },
    deleteMessage: (state, action) => {
      const { chatId, messageId } = action.payload
      const messages = state.messagesByChatId[chatId]
      if (messages) {
        state.messagesByChatId[chatId] = messages.filter(m => m.id !== messageId)
      }
    },
    setTyping: (state, action) => {
      state.isTyping = action.payload
    },
    setTypingUsers: (state, action) => {
      state.typingUsers = action.payload
    },
    setLoading: (state, action) => {
      state.isLoading = action.payload
    },
    clearMessages: (state, action) => {
      if (action.payload) {
        delete state.messagesByChatId[action.payload]
      } else {
        state.messagesByChatId = {}
      }
    },
  },
})

export const {
  setMessages,
  addMessage,
  updateMessage,
  deleteMessage,
  setTyping,
  setTypingUsers,
  setLoading,
  clearMessages,
} = messagesSlice.actions

export const sendMessage = (message) => async (dispatch) => {
  dispatch(addMessage(message))
}

export const fetchMessages = (chatId) => async (dispatch) => {
  // This will be implemented with actual API call
  dispatch(setLoading(true))
  try {
    // API call here
    // const messages = await chatService.getMessages(chatId)
    // dispatch(setMessages({ chatId, messages }))
  } catch (error) {
    console.error('Error fetching messages:', error)
  } finally {
    dispatch(setLoading(false))
  }
}

export default messagesSlice.reducer
