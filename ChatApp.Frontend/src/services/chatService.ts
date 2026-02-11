import { apiClient } from './apiClient';
import type { Chat, Message, User, SendMessageDto } from '../types/index';
import { generateId } from '../utils';

export const chatService = {
  getChats: async (): Promise<Chat[]> => {
    try {
      // Get all users first (excluding current user, filtered by backend)
      const users = await apiClient.get<User[]>('/api/chat/users');
      console.log(`💬 Fetched ${users.length} users to chat with`);
      
      if (users.length === 0) {
        console.log('ℹ️ No other users available to chat with');
        return [];
      }
      
      // Get recent messages to create chat list
      const recentMessages = await apiClient.get<any[]>('/api/chat/messages/recent');
      
      // Transform to chat format
      const chats: Chat[] = users.map((user: User) => {
        const recentMessage = recentMessages.find(msg => 
          msg.senderId === user.id || msg.receiverId === user.id
        );
        
        return {
          id: user.id,
          name: user.userName,
          type: 'private',
          participants: [user],
          lastMessage: recentMessage ? {
            id: generateId(),
            chatId: user.id,
            senderId: recentMessage.senderId,
            content: recentMessage.content,
            type: 'text',
            timestamp: recentMessage.createdAt || recentMessage.timestamp || new Date().toISOString(),
            status: 'read',
          } : undefined,
          unreadCount: 0, // TODO: Calculate from unread messages
          createdAt: new Date().toISOString(),
          updatedAt: recentMessage?.createdAt || recentMessage?.timestamp || new Date().toISOString(),
        };
      });
      
      console.log(`✅ Loaded ${chats.length} chats`);
      return chats;
    } catch (error: any) {
      console.error('❌ Error fetching chats:', error);
      if (error.response?.status === 401) {
        console.log('⚠️ Unauthorized - please log in again');
        return [];
      }
      return [];
    }
  },

  createChat: async (chatData: { name: string; type: 'private' | 'group'; participantIds: string[] }): Promise<Chat> => {
    // This would be implemented in the backend if needed
    const newChat: Chat = {
      id: generateId(),
      name: chatData.name,
      type: chatData.type,
      participants: [],
      unreadCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    return newChat;
  },

  getMessages: async (userId: string): Promise<Message[]> => {
    try {
      const messages = await apiClient.get<any[]>(`/api/chat/messages/${userId}`);
      
      return messages
        .map(msg => ({
          id: msg.id || generateId(), // Use actual message ID if available
          chatId: userId,
          senderId: msg.senderId,
          content: msg.content,
          type: 'text',
          timestamp: msg.createdAt || msg.timestamp || new Date().toISOString(),
          status: 'read',
        }))
        .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()); // Sort by timestamp ascending
    } catch (error: any) {
      console.error('Error fetching messages:', error);
      if (error.response?.status === 401 || error.response?.status === 404) {
        return [];
      }
      return [];
    }
  },

  sendMessage: async (messageData: { chatId: string; content: string; type: 'text' | 'image' | 'file' }): Promise<Message> => {
    const sendMessageDto: SendMessageDto = {
      receiverId: messageData.chatId, // Frontend keeps it as string
      content: messageData.content,
    };
    
    try {
      const response = await apiClient.post<any>('/api/chat/messages', sendMessageDto);
      
      return {
        id: generateId(),
        chatId: messageData.chatId,
        senderId: response.senderId || 'current_user', // Backend will populate this
        content: messageData.content,
        type: messageData.type,
        timestamp: response.createdAt || response.timestamp || new Date().toISOString(),
        status: 'sent',
      };
    } catch (error: any) {
      console.error('Error sending message via REST API:', error);
      // Don't re-throw, instead return a fallback message for better UX
      // The real message should be sent via SignalR anyway
      return {
        id: generateId(),
        chatId: messageData.chatId,
        senderId: 'current_user',
        content: messageData.content,
        type: messageData.type,
        timestamp: new Date().toISOString(),
        status: 'sent',
      };
    }
  },

  getUsers: async (): Promise<User[]> => {
    try {
      return await apiClient.get<User[]>('/api/chat/users');
    } catch (error: any) {
      console.error('Error fetching users:', error);
      if (error.response?.status === 401 || error.response?.status === 404) {
        return [];
      }
      return [];
    }
  },

  searchUsers: async (query: string): Promise<User[]> => {
    try {
      return await apiClient.get<User[]>(`/api/chat/users/search${query ? `?query=${encodeURIComponent(query)}` : ''}`);
    } catch (error: any) {
      console.error('Error searching users:', error);
      if (error.response?.status === 401 || error.response?.status === 404) {
        return [];
      }
      return [];
    }
  },
};