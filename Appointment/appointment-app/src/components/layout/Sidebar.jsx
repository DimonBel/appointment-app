import React from 'react'
import { Link } from 'react-router-dom'

export const Sidebar = ({ activeItem, onNavigate }) => {
  const navItems = [
    { id: 'bookings', label: 'My Bookings', icon: 'calendar', path: '/' },
    { id: 'doctors', label: 'Find Doctors', icon: 'users', path: '/doctors' },
    { id: 'profile', label: 'Profile', icon: 'user', path: '/profile' },
    { id: 'settings', label: 'Settings', icon: 'settings', path: '/settings' }
  ]

  return (
    <aside className="bg-white min-h-screen p-4">
      <div className="mb-8 px-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg flex items-center justify-center" style={{ backgroundColor: '#4DA3FF' }}>
            <svg 
              xmlns="http://www.w3.org/2000/svg" 
              width="20" 
              height="20" 
              viewBox="0 0 24 24" 
              fill="none" 
              stroke="white" 
              strokeWidth="2" 
              strokeLinecap="round" 
              strokeLinejoin="round"
            >
              <rect width="18" height="18" x="3" y="4" rx="2" ry="2"></rect>
              <line x1="16" x2="16" y1="2" y2="6"></line>
              <line x1="8" x2="8" y1="2" y2="6"></line>
              <line x1="3" x2="21" y1="10" y2="10"></line>
            </svg>
          </div>
          <span className="text-[18px] font-semibold text-gray-900">Appointments</span>
        </div>
      </div>

      <nav className="space-y-1">
        {navItems.map((item) => {
          const isActiveItem = activeItem === item.id
          
          return (
            <Link
              key={item.id}
              to={item.path}
              onClick={() => onNavigate(item.id)}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl text-[14px] font-medium transition-colors duration-200 ${
                isActiveItem
                  ? 'bg-gray-100 text-[#1E2A38]'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
              }`}
            >
              <svg 
                xmlns="http://www.w3.org/2000/svg" 
                width="18" 
                height="18" 
                viewBox="0 0 24 24" 
                fill="none" 
                stroke={isActiveItem ? '#1E2A38' : '#6B7280'} 
                strokeWidth="2" 
                strokeLinecap="round" 
                strokeLinejoin="round"
              >
                <path d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2z"></path>
              </svg>
              {item.label}
            </Link>
          )
        })}
      </nav>
    </aside>
  )
}
