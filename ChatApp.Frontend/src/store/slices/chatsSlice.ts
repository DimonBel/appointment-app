import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { Chat, ChatState } from '../../types/index';
import { chatService } from '../../services/chatService';

const initialState: ChatState = {
  chats: [],
  selectedChat: null,
  isLoading: false,
  searchQuery: '',
  filteredChats: [],
};

export const fetchChats = createAsyncThunk(
  'chats/fetchChats',
  async (_, { rejectWithValue }) => {
    try {
      const chats = await chatService.getChats();
      return chats;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const createChat = createAsyncThunk(
  'chats/createChat',
  async (chatData: { name: string; type: 'private' | 'group'; participantIds: string[] }, { rejectWithValue }) => {
    try {
      const chat = await chatService.createChat(chatData);
      return chat;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

const chatsSlice = createSlice({
  name: 'chats',
  initialState,
  reducers: {
    setSelectedChat: (state, action: PayloadAction<Chat | null>) => {
      // Only update if the chat ID is different to prevent unnecessary re-renders
      if (!state.selectedChat && !action.payload) {
        return;
      }
      if (state.selectedChat?.id !== action.payload?.id) {
        state.selectedChat = action.payload;
      }
    },
    setSearchQuery: (state, action: PayloadAction<string>) => {
      state.searchQuery = action.payload;
      state.filteredChats = state.chats.filter(chat =>
        chat.name.toLowerCase().includes(action.payload.toLowerCase())
      );
    },
    updateLastMessage: (state, action: PayloadAction<{ chatId: string; message: any }>) => {
      const { chatId, message } = action.payload;
      const chatIndex = state.chats.findIndex(chat => chat.id === chatId);
      if (chatIndex !== -1) {
        state.chats[chatIndex].lastMessage = message;
        state.chats[chatIndex].updatedAt = new Date().toISOString();
      }
    },
    incrementUnreadCount: (state, action: PayloadAction<string>) => {
      const chat = state.chats.find(chat => chat.id === action.payload);
      if (chat && chat.id !== state.selectedChat?.id) {
        chat.unreadCount += 1;
      }
    },
    clearUnreadCount: (state, action: PayloadAction<string>) => {
      const chat = state.chats.find(chat => chat.id === action.payload);
      if (chat) {
        chat.unreadCount = 0;
      }
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchChats.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(fetchChats.fulfilled, (state, action) => {
        state.isLoading = false;
        state.chats = action.payload;
        state.filteredChats = action.payload;
      })
      .addCase(fetchChats.rejected, (state) => {
        state.isLoading = false;
      })
      .addCase(createChat.fulfilled, (state, action) => {
        state.chats.unshift(action.payload);
        state.filteredChats.unshift(action.payload);
      });
  },
});

export const {
  setSelectedChat,
  setSearchQuery,
  updateLastMessage,
  incrementUnreadCount,
  clearUnreadCount,
} = chatsSlice.actions;

export default chatsSlice.reducer;