import React from 'react'

export const Badge = ({ 
  children, 
  variant = 'default',
  className = '' 
}) => {
  const variants = {
    default: 'bg-gray-100 text-gray-800',
    primary: 'bg-primary-accent text-white',
    success: 'bg-green-100 text-green-800',
    warning: 'bg-yellow-100 text-yellow-800',
    danger: 'bg-red-100 text-red-800',
    info: 'bg-blue-100 text-blue-800',
  }
  
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${variants[variant]} ${className}`}>
      {children}
    </span>
  )
}

export const StatusBadge = ({ status }) => {
  const statusConfig = {
    confirmed: { label: 'Confirmed', variant: 'success' },
    pending: { label: 'Pending', variant: 'warning' },
    cancelled: { label: 'Cancelled', variant: 'danger' },
    completed: { label: 'Completed', variant: 'info' },
  }
  
  const config = statusConfig[status] || { label: status, variant: 'default' }
  
  return (
    <Badge variant={config.variant}>{config.label}</Badge>
  )
}
