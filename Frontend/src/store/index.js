import { configureStore } from '@reduxjs/toolkit'
import { persistStore, persistReducer } from 'redux-persist'
import storage from 'redux-persist/lib/storage'
import authReducer from './slices/authSlice'
import chatsReducer from './slices/chatsSlice'
import messagesReducer from './slices/messagesSlice'
import appointmentsReducer from './slices/appointmentsSlice'
import uiReducer from './slices/uiSlice'
import notificationsReducer from './slices/notificationsSlice'
import friendsReducer from './slices/friendsSlice'

const authPersistConfig = {
  key: 'auth',
  storage,
  whitelist: ['user', 'token', 'refreshToken', 'isAuthenticated']
}

export const store = configureStore({
  reducer: {
    auth: persistReducer(authPersistConfig, authReducer),
    chats: chatsReducer,
    messages: messagesReducer,
    appointments: appointmentsReducer,
    ui: uiReducer,
    notifications: notificationsReducer,
    friends: friendsReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE'],
      },
    }),
})

export const persistor = persistStore(store)
