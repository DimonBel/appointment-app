import React from 'react'

export const Button = ({ 
  children, 
  variant = 'primary', 
  size = 'md', 
  className = '',
  disabled = false,
  onClick,
  type = 'button',
  ...props 
}) => {
  const baseStyles = 'inline-flex items-center justify-center font-medium rounded-lg transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed'
  
  const variants = {
    primary: 'bg-primary-dark text-white hover:bg-primary-light active:scale-95',
    secondary: 'bg-button-secondary text text-primary hover:bg-gray-300 active:scale-95',
    accent: 'bg-primary-accent text-white hover:opacity-90 active:scale-95',
    outline: 'border-2 border-primary-dark text-primary-dark hover:bg-primary-dark hover:text-white',
    ghost: 'text-primary-dark hover:bg-gray-100 active:scale-95',
    danger: 'bg-red-500 text-white hover:bg-red-600 active:scale-95',
  }
  
  const sizes = {
    sm: 'px-3 py-1.5 text-sm',
    md: 'px-4 py-2 text-base',
    lg: 'px-6 py-3 text-lg',
  }
  
  return (
    <button
      type={type}
      className={`${baseStyles} ${variants[variant]} ${sizes[size]} ${className}`}
      disabled={disabled}
      onClick={onClick}
      {...props}
    >
      {children}
    </button>
  )
}
