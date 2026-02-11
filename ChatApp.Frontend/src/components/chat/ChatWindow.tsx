import React from 'react';
import { ChatHeader } from './ChatHeader';
import { MessageList } from './MessageList';
import { MessageInput } from './MessageInput';
import type { Chat, Message, User } from '../../types';
import { cn } from '../../utils';

interface ChatWindowProps {
  chat: Chat | null;
  messages: Message[];
  currentUserId: string;
  users: Record<string, User>;
  typingUsers: User[];
  onSendMessage: (content: string) => void;
  onMenuClick: () => void;
  className?: string;
}

export const ChatWindow: React.FC<ChatWindowProps> = ({
  chat,
  messages,
  currentUserId,
  users,
  typingUsers,
  onSendMessage,
  onMenuClick,
  className,
}) => {
  if (!chat) {
    return (
      <div className={cn('flex-1 flex items-center justify-center bg-muted/20', className)}>
        <div className="text-center">
          <div className="w-16 h-16 bg-muted rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-muted-foreground" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z" />
            </svg>
          </div>
          <h3 className="text-lg font-semibold mb-2">Welcome to Chat</h3>
          <p className="text-muted-foreground">Select a conversation to start messaging</p>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('flex flex-col h-full bg-background', className)}>
      <ChatHeader
        chat={chat}
        onMenuClick={onMenuClick}
      />
      
      <MessageList
        messages={messages}
        currentUserId={currentUserId}
        users={users}
        typingUsers={typingUsers}
      />
      
      <MessageInput
        onSendMessage={onSendMessage}
      />
    </div>
  );
};