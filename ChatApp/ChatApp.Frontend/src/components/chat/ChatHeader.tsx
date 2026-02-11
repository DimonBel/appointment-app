import React from 'react';
import { Avatar, AvatarImage, AvatarFallback } from '../ui/Avatar';
import { MoreVertical, Phone, Video } from 'lucide-react';
import type { User } from '../../types';
import { cn } from '../../utils';

interface ChatHeaderProps {
  chat: {
    id: string;
    name: string;
    type: 'private' | 'group';
    participants: User[];
  };
  onMenuClick: () => void;
  className?: string;
}

export const ChatHeader: React.FC<ChatHeaderProps> = ({ chat, onMenuClick, className }) => {
  const otherUser = chat.type === 'private' ? chat.participants[0] : null;
  const displayName = otherUser ? otherUser.userName : chat.name;
  const displayAvatar = otherUser?.avatar || undefined;
  const isOnline = otherUser?.isOnline || false;

  return (
    <div className={cn('flex items-center justify-between p-4 border-b bg-background', className)}>
      <div className="flex items-center gap-3">
        <div className="relative">
          <Avatar className="h-10 w-10">
            <AvatarImage src={displayAvatar} alt={displayName} />
            <AvatarFallback className="font-semibold">
              {displayName.charAt(0).toUpperCase()}
            </AvatarFallback>
          </Avatar>
          {isOnline && (
            <div className="absolute bottom-0 right-0 h-3 w-3 bg-green-500 rounded-full border-2 border-background" />
          )}
        </div>

        <div>
          <h2 className="font-semibold text-lg">{displayName}</h2>
          <p className="text-sm text-muted-foreground">
            {isOnline ? 'Active now' : otherUser?.lastSeen ? `Last seen ${new Date(otherUser.lastSeen).toLocaleString()}` : 'Offline'}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-2">
        <button className="p-2 hover:bg-accent rounded-lg transition-colors">
          <Phone className="h-5 w-5" />
        </button>
        <button className="p-2 hover:bg-accent rounded-lg transition-colors">
          <Video className="h-5 w-5" />
        </button>
        <button 
          className="p-2 hover:bg-accent rounded-lg transition-colors"
          onClick={onMenuClick}
        >
          <MoreVertical className="h-5 w-5" />
        </button>
      </div>
    </div>
  );
};