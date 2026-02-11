import type { Message } from '../types/index';
import { chatService } from './chatService';

export const messageService = {
  getMessages: async (chatId: string): Promise<Message[]> => {
    return await chatService.getMessages(chatId);
  },

  sendMessage: async (messageData: { chatId: string; content: string; type: 'text' | 'image' | 'file' }): Promise<Message> => {
    return await chatService.sendMessage(messageData);
  },

  updateMessageStatus: async (_messageId: string, _status: 'sent' | 'delivered' | 'read'): Promise<void> => {
    // This would be implemented in the backend if needed
    return Promise.resolve();
  },
};