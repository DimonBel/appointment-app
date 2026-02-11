export interface User {
  id: string;
  userName: string;
  email: string;
  avatar?: string;
  isOnline: boolean;
  lastSeen?: string;
}

export interface Chat {
  id: string;
  name: string;
  type: 'private' | 'group';
  participants: User[];
  lastMessage?: Message;
  unreadCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  confirmPassword: string;
  userName: string;
}

export interface LoginDto {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface SendMessageDto {
  receiverId: string;
  content: string;
}

export interface Message {
  id: string;
  chatId: string;
  senderId: string;
  content: string;
  type: 'text' | 'image' | 'file';
  timestamp: string;
  status: 'sending' | 'sent' | 'delivered' | 'read';
  editedAt?: string;
}

export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
}

export interface ChatState {
  chats: Chat[];
  selectedChat: Chat | null;
  isLoading: boolean;
  searchQuery: string;
  filteredChats: Chat[];
}

export interface MessageState {
  messages: Record<string, Message[]>;
  isLoading: boolean;
  isTyping: Record<string, User[]>;
}

export interface UIState {
  theme: 'light' | 'dark' | 'system';
  sidebarOpen: boolean;
  showMobileProfile: boolean;
  notifications: Notification[];
}

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message: string;
  duration?: number;
}