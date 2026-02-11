import React from 'react';
import { Avatar, AvatarImage, AvatarFallback } from '../ui/Avatar';
import { Badge } from '../ui/Badge';
import { formatTime, truncateText } from '../../utils';
import type { Chat } from '../../types';

interface ChatListItemProps {
  chat: Chat;
  isSelected: boolean;
  onClick: () => void;
}

export const ChatListItem: React.FC<ChatListItemProps> = ({ chat, isSelected, onClick }) => {
  const otherUser = chat.type === 'private' ? chat.participants[0] : null;
  const displayName = otherUser ? otherUser.userName : chat.name;
  const displayAvatar = otherUser?.avatar || undefined;

  return (
    <div
      className={`
        flex items-center gap-3 p-3 cursor-pointer transition-all duration-200
        hover:bg-accent/50 rounded-lg group
        ${isSelected ? 'bg-accent' : ''}
      `}
      onClick={onClick}
    >
      <div className="relative">
        <Avatar className="h-12 w-12">
          <AvatarImage src={displayAvatar} alt={displayName} />
          <AvatarFallback className="text-lg font-semibold">
            {displayName.charAt(0).toUpperCase()}
          </AvatarFallback>
        </Avatar>
        {otherUser?.isOnline && (
          <div className="absolute bottom-0 right-0 h-3 w-3 bg-green-500 rounded-full border-2 border-background" />
        )}
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between mb-1">
          <h3 className="font-semibold text-sm truncate">{displayName}</h3>
          <span className="text-xs text-muted-foreground">
            {chat.lastMessage && formatTime(chat.lastMessage.timestamp)}
          </span>
        </div>

        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground truncate">
            {chat.lastMessage 
              ? truncateText(chat.lastMessage.content, 30)
              : 'No messages yet'
            }
          </p>
          {chat.unreadCount > 0 && (
            <Badge variant="destructive" className="ml-2 h-5 min-w-[20px] px-1 text-xs">
              {chat.unreadCount > 99 ? '99+' : chat.unreadCount}
            </Badge>
          )}
        </div>
      </div>
    </div>
  );
};