import React from 'react'

export const Avatar = ({ src, alt, size = 56, className = '' }) => {
  return (
    <div 
      className={`rounded-lg overflow-hidden ${className}`}
      style={{ 
        width: `${size}px`,
        height: `${size}px`
      }}
    >
      {src ? (
        <img 
          src={src} 
          alt={alt || 'Avatar'}
          className="w-full h-full object-cover"
        />
      ) : (
        <div className="w-full h-full bg-gray-300 flex items-center justify-center text-gray-600 text-sm font-medium">
          {alt?.charAt(0) || 'U'}
        </div>
      )}
    </div>
  )
}

export const AvatarGroup = ({ avatars, size = 40, max = 3 }) => {
  const visibleAvatars = avatars.slice(0, max)
  const remainingCount = avatars.length - max

  return (
    <div className="flex -space-x-2 overflow-hidden">
      {visibleAvatars.map((avatar, index) => (
        <Avatar
          key={index}
          src={avatar.src}
          alt={avatar.alt}
          size={size}
          className="border-2 border-white"
        />
      ))}
      {remainingCount > 0 && (
        <div
          className={`flex items-center justify-center rounded-full border-2 border-white bg-gray-400 text-white text-sm font-medium ${size > 40 ? 'px-3 py-0.5' : 'px-2 py-0.5'}`}
          style={{ 
            width: `${size}px`,
            height: `${size}px`
          }}
        >
          +{remainingCount}
        </div>
      )}
    </div>
  )
}
