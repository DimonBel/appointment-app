import React from 'react';
import { cn } from '../../utils';

interface AvatarProps extends React.HTMLAttributes<HTMLDivElement> {
  src?: string;
  alt?: string;
  fallback?: string;
  size?: 'sm' | 'md' | 'lg';
}

const Avatar = React.forwardRef<HTMLDivElement, AvatarProps>(
  ({ className, src, alt, fallback, size = 'md', ...props }, ref) => {
    const sizes = {
      sm: 'h-8 w-8 text-xs',
      md: 'h-10 w-10 text-sm',
      lg: 'h-12 w-12 text-base',
    };

    const initials = fallback || (alt ? alt.split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase() : '?');

    return (
      <div
        ref={ref}
        className={cn(
          'relative flex shrink-0 overflow-hidden rounded-full bg-primary/10',
          sizes[size],
          className
        )}
        {...props}
      >
        {src ? (
          <img
            className="aspect-square h-full w-full object-cover"
            src={src}
            alt={alt}
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center bg-primary/20 text-primary font-medium">
            {initials}
          </div>
        )}
      </div>
    );
  }
);

Avatar.displayName = 'Avatar';

export { Avatar };
