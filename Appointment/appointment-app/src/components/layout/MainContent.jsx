import React from 'react'

export const MainContent = ({ children, className = '' }) => {
  return (
    <main className={`${className} flex-1 overflow-auto bg-[#F2F2F2]`}>
      <div className="max-w-4xl mx-auto p-6">
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

export const SectionHeader = ({ title, subtitle, className = '' }) => {
  return (
    <div className={`mb-6 ${className}`}>
      {title && (
        <h1 className="text-[20px] font-semibold text-gray-900 mb-2">{title}</h1>
      )}
      {subtitle && (
        <p className="text-[14px] text-gray-500">{subtitle}</p>
      )}
    </div>
  )
}
