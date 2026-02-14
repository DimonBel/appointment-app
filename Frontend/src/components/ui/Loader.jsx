import React from 'react'

export const Loader = ({ 
  size = 'md',
  className = '' 
}) => {
  const sizes = {
    sm: 'w-4 h-4 border-2',
    md: 'w-8 h-8 border-2',
    lg: 'w-12 h-12 border-4',
  }
  
  return (
    <div className={`inline-block ${sizes[size]} border-primary-accent border-t-transparent rounded-full animate-spin ${className}`} />
  )
}

export const LoadingScreen = ({ message = 'Loading...' }) => {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen">
      <Loader size="lg" />
      <p className="mt-4 text-text-secondary">{message}</p>
    </div>
  )
}

export const LoadingOverlay = ({ message = 'Loading...' }) => {
  return (
    <div className="absolute inset-0 bg-white/80 flex flex-col items-center justify-center z-50">
      <Loader size="lg" />
      <p className="mt-4 text-text-secondary">{message}</p>
    </div>
  )
}
