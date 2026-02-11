import React from 'react';
import { Avatar, AvatarImage, AvatarFallback } from '../ui/Avatar';
import { formatTime } from '../../utils';
import type { Message } from '../../types';
import { cn } from '../../utils';

interface MessageBubbleProps {
  message: Message;
  isOwn: boolean;
  senderName?: string;
  senderAvatar?: string;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({
  message,
  isOwn,
  senderName,
  senderAvatar,
}) => {
  const getStatusIcon = () => {
    if (!isOwn) return null;
    
    switch (message.status) {
      case 'sending':
        return <span className="text-xs text-muted-foreground">⏳</span>;
      case 'sent':
        return <span className="text-xs text-muted-foreground">✓</span>;
      case 'delivered':
        return <span className="text-xs text-muted-foreground">✓✓</span>;
      case 'read':
        return <span className="text-xs text-blue-500">✓✓</span>;
      default:
        return null;
    }
  };

  return (
    <div
      className={cn(
        'flex gap-2 mb-4 animate-fade-in',
        isOwn ? 'flex-row-reverse' : 'flex-row'
      )}
    >
      {!isOwn && senderAvatar && (
        <Avatar className="h-8 w-8 mt-1">
          <AvatarImage src={senderAvatar} alt={senderName} />
          <AvatarFallback className="text-xs">
            {senderName?.charAt(0).toUpperCase()}
          </AvatarFallback>
        </Avatar>
      )}

      <div
        className={cn(
          'message-bubble',
          isOwn ? 'flex flex-col items-end' : 'flex flex-col items-start'
        )}
      >
        {!isOwn && senderName && (
          <p className="text-xs text-muted-foreground mb-1 px-1">{senderName}</p>
        )}
        
        <div
          className={cn(
            'px-4 py-2 rounded-2xl break-words max-w-full',
            isOwn
              ? 'bg-primary text-primary-foreground rounded-br-sm'
              : 'bg-muted rounded-bl-sm'
          )}
        >
          <p className="text-sm leading-relaxed">{message.content}</p>
        </div>

        <div className={cn(
          'flex items-center gap-1 mt-1 px-1',
          isOwn ? 'flex-row-reverse' : 'flex-row'
        )}>
          <span className="text-xs text-muted-foreground">
            {formatTime(message.timestamp)}
          </span>
          {getStatusIcon()}
        </div>
      </div>
    </div>
  );
};