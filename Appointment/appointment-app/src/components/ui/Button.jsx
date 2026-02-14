import React from 'react'
import { Icon } from './Icon'

export const Button = ({ 
  children, 
  variant = 'primary', 
  size = 'medium', 
  className = '', 
  onClick,
  type = 'button',
  disabled = false,
  icon: IconName
}) => {
  const baseStyles = 'font-medium rounded-full transition-all duration-200 flex items-center justify-center'
  
  const sizeStyles = {
    small: 'h-8 px-4 text-sm',
    medium: 'h-10 px-5 text-base',
    large: 'h-12 px-6 text-lg'
  }
  
  const variantStyles = {
    primary: 'bg-[#1E2A38] text-white hover:bg-[#2C3E50] disabled:bg-gray-400 disabled:cursor-not-allowed',
    secondary: 'bg-[#E5E7EB] text-gray-900 hover:bg-gray-300 disabled:bg-gray-200 disabled:cursor-not-allowed',
    ghost: 'bg-transparent text-gray-700 hover:bg-gray-100 disabled:bg-transparent disabled:text-gray-400 disabled:cursor-not-allowed'
  }
  
  const iconSpacing = IconName ? 'mr-2' : ''
  
  return (
    <button
      type={type}
      className={`${baseStyles} ${sizeStyles[size]} ${variantStyles[variant]} ${iconSpacing} ${className}`}
      onClick={onClick}
      disabled={disabled}
    >
      {IconName && <Icon name={IconName} size={16} />}
      {children}
    </button>
  )
}

export const ButtonGroup = ({ children, className = '' }) => {
  return (
    <div className={`flex gap-3 ${className}`}>
      {children}
    </div>
  )
}
