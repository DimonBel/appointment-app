import { configureStore } from '@reduxjs/toolkit';
import { persistStore, persistReducer } from 'redux-persist';
import storage from 'redux-persist/lib/storage'; // defaults to localStorage for web
import authSlice from './slices/authSlice';
import chatsSlice from './slices/chatsSlice';
import messagesSlice from './slices/messagesSlice';
import uiSlice from './slices/uiSlice';

// Persist config for auth slice
const authPersistConfig = {
  key: 'auth',
  storage,
  whitelist: ['user', 'isAuthenticated'] // only persist these fields
};

const rootReducer = {
  auth: persistReducer(authPersistConfig, authSlice),
  chats: chatsSlice,
  messages: messagesSlice,
  ui: uiSlice,
};

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE', 'persist/FLUSH', 'persist/PAUSE', 'persist/PURGE'],
      },
    }),
});

export const persistor = persistStore(store);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;