import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';
import type { Message, MessageState, User } from '../../types/index';
import { messageService } from '../../services/messageService';

const initialState: MessageState = {
  messages: {},
  isLoading: false,
  isTyping: {},
};

export const fetchMessages = createAsyncThunk(
  'messages/fetchMessages',
  async (chatId: string, { rejectWithValue }) => {
    try {
      const messages = await messageService.getMessages(chatId);
      return { chatId, messages };
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const sendMessage = createAsyncThunk(
  'messages/sendMessage',
  async (messageData: { chatId: string; content: string; type: 'text' | 'image' | 'file'; senderId?: string }, { rejectWithValue }) => {
    try {
      // Use the messageService for REST API fallback
      const message = await messageService.sendMessage(messageData);
      return message;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

const messagesSlice = createSlice({
  name: 'messages',
  initialState,
  reducers: {
    addMessage: (state, action: PayloadAction<Message>) => {
      const message = action.payload;
      if (!state.messages[message.chatId]) {
        state.messages[message.chatId] = [];
      }
      // Check if message already exists to prevent duplicates
      const exists = state.messages[message.chatId].some(msg => msg.id === message.id);
      if (!exists) {
        state.messages[message.chatId].push(message);
      }
    },
    updateMessageStatus: (state, action: PayloadAction<{ messageId: string; status: 'sent' | 'delivered' | 'read' }>) => {
      const { messageId, status } = action.payload;
      Object.values(state.messages).forEach(messages => {
        const message = messages.find(msg => msg.id === messageId);
        if (message) {
          message.status = status;
        }
      });
    },
    setTyping: (state, action: PayloadAction<{ chatId: string; users: User[] }>) => {
      const { chatId, users } = action.payload;
      state.isTyping[chatId] = users;
    },
    clearTyping: (state, action: PayloadAction<string>) => {
      delete state.isTyping[action.payload];
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMessages.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(fetchMessages.fulfilled, (state, action) => {
        state.isLoading = false;
        const { chatId, messages } = action.payload;
        console.log('📚 Messages fetched for chat', chatId, ':', messages);
        state.messages[chatId] = messages;
      })
      .addCase(fetchMessages.rejected, (state, action) => {
        state.isLoading = false;
        console.error('❌ Failed to fetch messages:', action.payload);
      })
      .addCase(sendMessage.fulfilled, (state, action) => {
        const message = action.payload;
        if (!state.messages[message.chatId]) {
          state.messages[message.chatId] = [];
        }
        state.messages[message.chatId].push(message);
      });
  },
});

export const {
  addMessage,
  updateMessageStatus,
  setTyping,
  clearTyping,
} = messagesSlice.actions;

export default messagesSlice.reducer;