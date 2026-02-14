import React from 'react'

export const Avatar = ({ 
  src, 
  alt = '', 
  size = 40,
  className = '',
  fallback = null
}) => {
  const [imageError, setImageError] = React.useState(false)
  
  const handleError = () => {
    setImageError(true)
  }
  
  // Extract initials from alt text
  const getInitials = () => {
    if (!alt) return '?'
    const parts = alt.split(' ')
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[1][0]}`.toUpperCase()
    }
    return alt[0].toUpperCase()
  }
  
  return (
    <div 
      className={`relative inline-flex items-center justify-center rounded-full bg-primary-accent overflow-hidden ${className}`}
      style={{ width: size, height: size }}
    >
      {src && !imageError ? (
        <img 
          src={src} 
          alt={alt}
          onError={handleError}
          className="w-full h-full object-cover"
        />
      ) : fallback ? (
        fallback
      ) : (
        <span className="text-white font-semibold" style={{ fontSize: size / 2.5 }}>
          {getInitials()}
        </span>
      )}
    </div>
  )
}
