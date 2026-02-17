import { createSlice } from '@reduxjs/toolkit'

const initialState = {
  notifications: [],
  unreadCount: 0,
  isLoading: false,
  error: null,
  preferences: [],
}

const notificationsSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    setNotifications: (state, action) => {
      state.notifications = action.payload
      state.isLoading = false
    },
    addNotification: (state, action) => {
      state.notifications.unshift(action.payload)
      const status = action.payload?.status
      const isRead = status === 'Read' || status === 2
      if (!isRead) {
        state.unreadCount += 1
      }
    },
    markNotificationAsRead: (state, action) => {
      const notification = state.notifications.find(n => n.id === action.payload)
      if (notification && notification.status !== 'Read') {
        notification.status = 'Read'
        state.unreadCount = Math.max(0, state.unreadCount - 1)
      }
    },
    markAllAsRead: (state) => {
      state.notifications.forEach(n => { n.status = 'Read' })
      state.unreadCount = 0
    },
    removeNotification: (state, action) => {
      const idx = state.notifications.findIndex(n => n.id === action.payload)
      if (idx !== -1) {
        if (state.notifications[idx].status !== 'Read') {
          state.unreadCount = Math.max(0, state.unreadCount - 1)
        }
        state.notifications.splice(idx, 1)
      }
    },
    setUnreadCount: (state, action) => {
      state.unreadCount = action.payload
    },
    setLoading: (state, action) => {
      state.isLoading = action.payload
    },
    setError: (state, action) => {
      state.error = action.payload
      state.isLoading = false
    },
    setPreferences: (state, action) => {
      state.preferences = action.payload
    },
  },
})

export const {
  setNotifications,
  addNotification,
  markNotificationAsRead,
  markAllAsRead,
  removeNotification,
  setUnreadCount,
  setLoading,
  setError,
  setPreferences,
} = notificationsSlice.actions

export default notificationsSlice.reducer
