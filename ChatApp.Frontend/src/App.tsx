import { useEffect, useState } from 'react';
import { Provider } from 'react-redux';
import { PersistGate } from 'redux-persist/integration/react';
import { store, persistor } from './store';
import { useAuth, useChats, useAppSelector, useAppDispatch } from './hooks';
import { AuthForm } from './components/auth/AuthForm';
import { ChatList } from './components/chat/ChatList';
import { ChatWindow } from './components/chat/ChatWindow';
import { setTheme } from './store/slices/uiSlice';
import { sendMessage, addMessage, fetchMessages } from './store/slices/messagesSlice';
import { setSearchQuery, clearUnreadCount } from './store/slices/chatsSlice';
import { Menu, X, Moon, Sun } from 'lucide-react';
import { Button } from './components/ui/Button';
import { signalRService } from './services/signalRService';
import { authService } from './services/authService';

function AppContent() {
  const { isAuthenticated, isLoading, login, register, checkAuthStatus: checkAuth } = useAuth();
  const { selectedChat, filteredChats, selectChat } = useChats();
  const messages = useAppSelector(state => state.messages);
  const user = useAppSelector(state => state.auth.user);
  const theme = useAppSelector(state => state.ui.theme);
  const sidebarOpen = useAppSelector(state => state.ui.sidebarOpen);
  const isTyping = useAppSelector(state => state.messages.isTyping);
  const currentUserId = user?.id;
  const dispatch = useAppDispatch();
  
  const [isLoginMode, setIsLoginMode] = useState(true);
  const [checkingAuth, setCheckingAuth] = useState(true);

  useEffect(() => {
    dispatch(setTheme('system'));
  }, [dispatch]);

  // Check authentication status on app load and after rehydration
  useEffect(() => {
    const handleRehydration = setTimeout(async () => {
      await checkAuth();
      setCheckingAuth(false);
    }, 100); // Small delay to ensure rehydration is complete

    return () => clearTimeout(handleRehydration);
  }, [checkAuth]);

  useEffect(() => {
    // Only connect SignalR when we have a user ID (user object might change but ID should be stable)
    if (!currentUserId || !isAuthenticated) {
      console.log('🔴 Cannot connect SignalR - missing user ID or not authenticated');
      return;
    }

    console.log('✅ User authenticated, connecting SignalR...', { currentUserId, userName: user?.userName });
    
    // Set up message handlers BEFORE connecting
    const handleMessageReceive = (data: { senderId: string; message: string; messageId: string; createdAt: string }) => {
      console.log('🔔 Processing received message:', data);
      const messageObj = {
        id: data.messageId || `signalr_${Date.now()}_${Math.random()}`,
        chatId: data.senderId.toString(), // Convert to string for frontend compatibility
        senderId: data.senderId.toString(),
        content: data.message,
        type: 'text' as const,
        timestamp: data.createdAt || new Date().toISOString(),
        status: 'delivered' as const,
      };
      console.log('📝 Adding received message to Redux:', messageObj);
      dispatch(addMessage(messageObj));
    };

    const handleMessageSent = (data: { receiverId: string; message: string; messageId: string; createdAt: string }) => {
      console.log('✅ Message sent confirmation received:', data);
      const messageObj = {
        id: data.messageId || `signalr_${Date.now()}_${Math.random()}`,
        chatId: data.receiverId.toString(),
        senderId: currentUserId,
        content: data.message,
        type: 'text' as const,
        timestamp: data.createdAt || new Date().toISOString(),
        status: 'delivered' as const,
      };
      console.log('📝 Adding sent message to Redux:', messageObj);
      dispatch(addMessage(messageObj));
    };

    const handleUserTyping = (data: { senderId: string }) => {
      const user = { id: data.senderId, userName: '', email: '', isOnline: true };
      dispatch({ type: 'messages/setTyping', payload: { chatId: data.senderId, users: [user] } });
    };

    // Register handlers
    signalRService.onMessage('ReceiveMessage', handleMessageReceive);
    signalRService.onMessage('MessageSent', handleMessageSent);
    signalRService.onMessage('UserTyping', handleUserTyping);
    
    // Add delay to ensure authentication is fully processed
    const connectTimer = setTimeout(() => {
      signalRService.connect().catch(err => {
        console.error('❌ Failed to connect SignalR:', err);
      });
    }, 1000);
    
    return () => {
      clearTimeout(connectTimer);
      console.log('🔌 Disconnecting SignalR...');
      signalRService.disconnect();
    };
  }, [currentUserId, dispatch, isAuthenticated, user?.userName]);

  // Fetch current user when authenticated
  useEffect(() => {
    if (isAuthenticated && !user) {
      console.log('🔍 Getting current user details...');
      authService.getCurrentUser().then(currentUser => {
        if (currentUser) {
          console.log('✅ Current user retrieved:', currentUser);
          dispatch({ type: 'auth/setUser', payload: currentUser });
        } else {
          console.warn('⚠️ No current user found despite being authenticated');
        }
      });
    }
  }, [isAuthenticated, user, dispatch]);

  // Fetch messages when a chat is selected (only if not already fetched)
  useEffect(() => {
    if (selectedChat && isAuthenticated) {
      const chatId = selectedChat.id;
      const existingMessages = (messages.messages as any)[chatId];
      
      // Only fetch if messages haven't been loaded yet
      if (!existingMessages || existingMessages.length === 0) {
        console.log('📚 Fetching messages for chat:', chatId);
        dispatch(fetchMessages(chatId));
      }
    }
  }, [selectedChat?.id, dispatch, isAuthenticated]); // Remove messages.messages from dependencies to prevent infinite loops

  const handleSendMessage = async (content: string) => {
    if (!selectedChat || !currentUserId) return;

    try {
      // Convert string ID to proper format for SignalR
      const receiverId = selectedChat.id;
      console.log(`🎯 Sending message to chat: ${receiverId}`);
      
      // Send via SignalR - the message will be added to UI when we receive MessageSent confirmation
      try {
        await signalRService.sendMessage(receiverId, content);
        console.log('✅ Message sent via SignalR');
      } catch (signalRError) {
        console.warn('⚠️ SignalR send failed, trying REST API fallback:', signalRError);
        // Fallback: Try to send via REST API if SignalR fails
        try {
          const messageData = {
            chatId: selectedChat.id,
            content,
            type: 'text' as const
          };
          await dispatch(sendMessage(messageData)).unwrap();
          console.log('✅ Message sent via REST API fallback');
        } catch (restError) {
          console.error('❌ Both SignalR and REST API failed:', restError);
        }
      }
    } catch (error) {
      console.error('Failed to send message:', error);
    }
  };

  const handleSearchChange = (query: string) => {
    dispatch(setSearchQuery(query));
  };

  const toggleSidebar = () => {
    dispatch({ type: 'ui/toggleSidebar' });
  };

  const toggleTheme = () => {
    const newTheme = theme === 'light' ? 'dark' : 'light';
    dispatch(setTheme(newTheme));
  };

  // Show loading state while checking authentication
  if (checkingAuth) {
    return (
      <div className="h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <div className="inline-block animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary mb-4"></div>
          <p className="text-lg font-medium">Loading...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
        <AuthForm
          isLogin={isLoginMode}
          onSubmit={async (data) => {
            if ('username' in data) {
              await register(data as any);
            } else {
              await login(data);
            }
          }}
          isLoading={isLoading}
          onToggleMode={() => setIsLoginMode(!isLoginMode)}
        />
    );
  }

  const selectedChatMessages = selectedChat ? (messages.messages as any)[selectedChat.id] || [] : [];
  const typingUsers = selectedChat ? (isTyping as any)[selectedChat.id] || [] : [];

  return (
    <div className="h-screen flex bg-background">
      {/* Sidebar */}
      <div className={`${sidebarOpen ? 'w-80' : 'w-0'} transition-all duration-300 overflow-hidden border-r`}>
        <ChatList
          chats={filteredChats}
          selectedChatId={selectedChat?.id || null}
          onChatSelect={(chat) => {
            selectChat(chat);
            dispatch(clearUnreadCount(chat.id));
          }}
          searchQuery=""
          onSearchChange={handleSearchChange}
        />
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col">
        {/* Top Bar */}
        <div className="flex items-center justify-between p-4 border-b bg-background">
          <div className="flex items-center gap-3">
            <Button variant="ghost" size="icon" onClick={toggleSidebar}>
              {sidebarOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </Button>
            <h1 className="text-xl font-semibold">Chat App</h1>
          </div>

          <div className="flex items-center gap-4">
            {/* Display user's nickname/username */}
            {user && (
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium hidden md:block">
                  Welcome, {user.userName}!
                </span>
              </div>
            )}
            
            <Button variant="ghost" size="icon" onClick={toggleTheme}>
              {theme === 'dark' ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            </Button>
          </div>
        </div>

        {/* Chat Window */}
        <ChatWindow
          chat={selectedChat}
          messages={selectedChatMessages}
          currentUserId={currentUserId || ''}
          users={{}}
          typingUsers={typingUsers}
          onSendMessage={handleSendMessage}
          onMenuClick={() => {}}
        />
      </div>
    </div>
  );
}

export default function App() {
  return (
    <Provider store={store}>
      <PersistGate 
        loading={
          <div className="h-screen flex items-center justify-center bg-background">
            <div className="text-center">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-primary mb-4"></div>
              <p className="text-lg font-medium">Loading...</p>
            </div>
          </div>
        } 
        persistor={persistor}
      >
        <AppContent />
      </PersistGate>
    </Provider>
  );
}