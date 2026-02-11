import React, { useState, useEffect } from 'react';
import { ScrollArea } from '../ui/ScrollArea';
import { Input } from '../ui/Input';
import { Search, Plus, X, User as UserIcon, Loader2 } from 'lucide-react';
import { ChatListItem } from './ChatListItem';
import { Avatar, AvatarImage, AvatarFallback } from '../ui/Avatar';
import type { Chat, User } from '../../types';
import { cn } from '../../utils';
import { chatService } from '../../services/chatService';

interface ChatListProps {
  chats: Chat[];
  selectedChatId: string | null;
  onChatSelect: (chat: Chat) => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  className?: string;
}

export const ChatList: React.FC<ChatListProps> = ({
  chats,
  selectedChatId,
  onChatSelect,
  searchQuery,
  onSearchChange,
  className,
}) => {
  const [showUserSearch, setShowUserSearch] = useState(false);
  const [searchUsersQuery, setSearchUsersQuery] = useState('');
  const [searchResults, setSearchResults] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleUserSearch = async () => {
    setLoading(true);
    setError(null);
    try {
      const users = await chatService.searchUsers(searchUsersQuery);
      setSearchResults(users);
    } catch (error: any) {
      console.error('Error searching users:', error);
      setError(error.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  // When showUserSearch changes or searchUsersQuery changes, perform the search
  useEffect(() => {
    if (showUserSearch) {
      handleUserSearch();
    }
  }, [showUserSearch, searchUsersQuery]);

  const handleStartChat = (user: User) => {
    // Create a temporary chat object for the selected user
    const newChat: Chat = {
      id: user.id,
      name: user.userName,
      type: 'private',
      participants: [user],
      unreadCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    
    onChatSelect(newChat);
    setShowUserSearch(false);
    setSearchUsersQuery('');
    setSearchResults([]);
  };

  return (
    <div className={cn('flex flex-col h-full bg-background border-r', className)}>
      <div className="p-4 border-b">
        {showUserSearch ? (
          <div className="mb-4">
            <div className="flex items-center gap-2 mb-3">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search users..."
                  value={searchUsersQuery}
                  onChange={(e) => setSearchUsersQuery(e.target.value)}
                  className="pl-10"
                  autoFocus
                />
              </div>
              <button 
                className="p-2 hover:bg-accent rounded-lg transition-colors"
                onClick={() => {
                  setShowUserSearch(false);
                  setSearchUsersQuery('');
                  setSearchResults([]);
                }}
              >
                <X className="h-5 w-5" />
              </button>
            </div>
            
            {loading ? (
              <div className="flex items-center justify-center py-4">
                <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
              </div>
            ) : error ? (
              <div className="p-4 text-center text-sm text-destructive">
                {error}
              </div>
            ) : (
              <ScrollArea className="max-h-60">
                <div className="space-y-2">
                  {searchResults.length === 0 ? (
                    <div className="p-4 text-center text-sm text-muted-foreground">
                      <UserIcon className="h-8 w-8 mx-auto mb-2 opacity-50" />
                      <p className="mb-1">{searchUsersQuery ? 'No users found' : 'No other users available'}</p>
                      <p className="text-xs">Create another account to test the chat</p>
                    </div>
                  ) : (
                    searchResults.map((user) => (
                      <div
                        key={user.id}
                        className="flex items-center gap-3 p-2 cursor-pointer hover:bg-accent rounded-lg transition-colors"
                        onClick={() => handleStartChat(user)}
                      >
                        <Avatar className="h-10 w-10">
                          <AvatarImage src={user.avatar} alt={user.userName} />
                          <AvatarFallback className="text-sm font-semibold">
                            {user.userName.charAt(0).toUpperCase()}
                          </AvatarFallback>
                        </Avatar>
                        <div className="flex-1 min-w-0">
                          <h3 className="font-medium text-sm truncate">{user.userName}</h3>
                          <p className="text-xs text-muted-foreground truncate">{user.email}</p>
                        </div>
                        {user.isOnline && (
                          <div className="h-2 w-2 bg-green-500 rounded-full"></div>
                        )}
                      </div>
                    ))
                  )}
                </div>
              </ScrollArea>
            )}
          </div>
        ) : (
          <div className="flex items-center gap-2 mb-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search chats..."
                value={searchQuery}
                onChange={(e) => onSearchChange(e.target.value)}
                className="pl-10"
              />
            </div>
            <button 
              className="p-2 hover:bg-accent rounded-lg transition-colors"
              onClick={() => setShowUserSearch(true)}
            >
              <Plus className="h-5 w-5" />
            </button>
          </div>
        )}
      </div>

      <ScrollArea className="flex-1">
        <div className="p-2">
          {!showUserSearch && chats.length === 0 ? (
            <div className="text-center py-12 px-4">
              <UserIcon className="h-16 w-16 mx-auto mb-4 text-muted-foreground opacity-50" />
              <p className="text-muted-foreground font-medium mb-2">No chats available</p>
              <p className="text-sm text-muted-foreground mb-4">
                Click the <Plus className="inline h-4 w-4 mx-1" /> button above to find users
              </p>
              <p className="text-xs text-muted-foreground">
                Note: You need at least 2 user accounts to test the chat
              </p>
            </div>
          ) : !showUserSearch && (
            chats.map((chat) => (
              <ChatListItem
                key={chat.id}
                chat={chat}
                isSelected={chat.id === selectedChatId}
                onClick={() => onChatSelect(chat)}
              />
            ))
          )}
        </div>
      </ScrollArea>
    </div>
  );
};