import React from 'react';
import { ScrollArea } from '../ui/ScrollArea';
import { MessageBubble } from './MessageBubble';
import { TypingIndicator } from './TypingIndicator';
import { Avatar, AvatarImage, AvatarFallback } from '../ui/Avatar';
import type { Message, User } from '../../types';
import { cn } from '../../utils';

interface MessageListProps {
  messages: Message[];
  currentUserId: string;
  users: Record<string, User>;
  typingUsers: User[];
  className?: string;
}

export const MessageList: React.FC<MessageListProps> = ({
  messages,
  currentUserId,
  users,
  typingUsers,
  className,
}) => {
  const messagesEndRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const groupMessagesByDate = (messages: Message[]) => {
    const groups: { [date: string]: Message[] } = {};
    
    messages.forEach((message) => {
      const date = new Date(message.timestamp).toDateString();
      if (!groups[date]) {
        groups[date] = [];
      }
      groups[date].push(message);
    });

    return groups;
  };

  const messageGroups = groupMessagesByDate(messages);

  return (
    <ScrollArea className={cn('flex-1 p-4', className)}>
      <div className="space-y-4">
        {Object.entries(messageGroups).map(([date, dateMessages]) => (
          <div key={date}>
            <div className="flex items-center justify-center my-4">
              <div className="bg-muted px-3 py-1 rounded-full">
                <span className="text-xs text-muted-foreground">
                  {new Date(date).toLocaleDateString('en-US', {
                    weekday: 'long',
                    month: 'short',
                    day: 'numeric',
                  })}
                </span>
              </div>
            </div>

            {dateMessages.map((message, index) => {
              const isOwn = message.senderId === currentUserId;
              const sender = users[message.senderId];
              const showAvatar = !isOwn && (
                index === 0 || 
                dateMessages[index - 1].senderId !== message.senderId
              );

              return (
                <MessageBubble
                  key={message.id}
                  message={message}
                  isOwn={isOwn}
                  senderName={sender?.userName}
                  senderAvatar={showAvatar ? sender?.avatar : undefined}
                />
              );
            })}
          </div>
        ))}

        {typingUsers.length > 0 && (
          <div className="flex gap-2 mb-4">
            {typingUsers.map((user) => (
              <div key={user.id} className="flex items-center gap-2 bg-muted px-3 py-2 rounded-2xl">
                <Avatar className="h-6 w-6">
                  <AvatarImage src={user.avatar} alt={user.userName} />
                  <AvatarFallback className="text-xs">
                    {user.userName.charAt(0).toUpperCase()}
                  </AvatarFallback>
                </Avatar>
                <TypingIndicator />
              </div>
            ))}
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>
    </ScrollArea>
  );
};