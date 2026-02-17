import { createSlice } from '@reduxjs/toolkit'

const initialState = {
  chats: [],
  selectedChatId: null,
  searchQuery: '',
  isLoading: false,
}

const chatsSlice = createSlice({
  name: 'chats',
  initialState,
  reducers: {
    setChats: (state, action) => {
      state.chats = action.payload
    },
    addChat: (state, action) => {
      const exists = state.chats.find(chat => chat.id === action.payload.id)
      if (!exists) {
        state.chats.unshift(action.payload)
      }
    },
    updateChat: (state, action) => {
      const index = state.chats.findIndex(chat => chat.id === action.payload.id)
      if (index !== -1) {
        state.chats[index] = { ...state.chats[index], ...action.payload }
      }
    },
    selectChat: (state, action) => {
      state.selectedChatId = action.payload
    },
    setSearchQuery: (state, action) => {
      state.searchQuery = action.payload
    },
    clearUnreadCount: (state, action) => {
      const chat = state.chats.find(c => c.id === action.payload)
      if (chat) {
        chat.unreadCount = 0
      }
    },
    setLoading: (state, action) => {
      state.isLoading = action.payload
    },
  },
})

export const {
  setChats,
  addChat,
  updateChat,
  selectChat,
  setSearchQuery,
  clearUnreadCount,
  setLoading,
} = chatsSlice.actions

export default chatsSlice.reducer
