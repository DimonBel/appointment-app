import React from 'react'

export const Card = ({ children, className = '', elevation = 'medium' }) => {
  const elevationStyles = {
    none: 'shadow-none',
    light: 'shadow-sm',
    medium: 'shadow-[0px_4px_12px_rgba(0,0,0,0.08)]',
    hover: 'shadow-md hover:shadow-lg transition-shadow duration-200'
  }

  return (
    <div className={`bg-white rounded-2xl ${elevationStyles[elevation]} ${className}`}>
      {children}
    </div>
  )
}

export const CardHeader = ({ children, className = '' }) => {
  return <div className={`p-4 border-b border-[#EAEAEA] ${className}`}>{children}</div>
}

export const CardBody = ({ children, className = '' }) => {
  return <div className={`p-4 ${className}`}>{children}</div>
}

export const CardFooter = ({ children, className = '' }) => {
  return <div className={`p-4 pt-0 ${className}`}>{children}</div>
}

export const CardTitle = ({ children, className = '' }) => {
  return <h3 className={`text-[16px] font-semibold text-gray-900 ${className}`}>{children}</h3>
}

export const CardSubtitle = ({ children, className = '' }) => {
  return <p className={`text-[14px] text-gray-600 ${className}`}>{children}</p>
}
