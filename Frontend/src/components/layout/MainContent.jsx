import React from 'react'

export const MainContent = ({ children, className = '' }) => {
  return (
    <main className={`${className} flex-1 overflow-auto bg-background-app`}>
      <div className="max-w-7xl mx-auto p-6">
        {children}
      </div>
    </main>
  )
}

export const Section = ({ children, className = '' }) => {
  return (
    <section className={`mb-8 ${className}`}>
      {children}
    </section>
  )
}

export const SectionHeader = ({ title, subtitle, action, className = '' }) => {
  return (
    <div className={`mb-6 flex items-center justify-between ${className}`}>
      <div>
        {title && (
          <h1 className="text-[20px] font-semibold text-text-primary mb-1">{title}</h1>
        )}
        {subtitle && (
          <p className="text-[14px] text-text-secondary">{subtitle}</p>
        )}
      </div>
      {action && (
        <div>
          {action}
        </div>
      )}
    </div>
  )
}
