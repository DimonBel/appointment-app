import React, { useState, useRef, useEffect } from 'react';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { Send, Paperclip, Smile } from 'lucide-react';
import { cn } from '../../utils';

interface MessageInputProps {
  onSendMessage: (content: string) => void;
  disabled?: boolean;
  className?: string;
}

export const MessageInput: React.FC<MessageInputProps> = ({
  onSendMessage,
  disabled = false,
  className,
}) => {
  const [message, setMessage] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  const handleSend = () => {
    if (message.trim() && !disabled) {
      onSendMessage(message.trim());
      setMessage('');
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  return (
    <div className={cn('flex items-center gap-2 p-4 border-t bg-background', className)}>
      <Button
        variant="ghost"
        size="icon"
        className="shrink-0"
        disabled={disabled}
      >
        <Paperclip className="h-5 w-5" />
      </Button>

      <div className="flex-1 relative">
        <Input
          ref={inputRef}
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Type a message..."
          disabled={disabled}
          className="pr-10"
        />
      </div>

      <Button
        variant="ghost"
        size="icon"
        className="shrink-0"
        disabled={disabled}
      >
        <Smile className="h-5 w-5" />
      </Button>

      <Button
        onClick={handleSend}
        disabled={!message.trim() || disabled}
        size="icon"
        className="shrink-0"
      >
        <Send className="h-4 w-4" />
      </Button>
    </div>
  );
};