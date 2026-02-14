import React from 'react'
import { Link } from 'react-router-dom'
import { Calendar, Users, MessageCircle, User, Settings } from 'lucide-react'

export const Sidebar = ({ activeItem, onNavigate }) => {
  const navItems = [
    { id: 'bookings', label: 'My Bookings', Icon: Calendar, path: '/' },
    { id: 'doctors', label: 'Find Doctors', Icon: Users, path: '/doctors' },
    { id: 'chat', label: 'Messages', Icon: MessageCircle, path: '/chat' },
    { id: 'profile', label: 'Profile', Icon: User, path: '/profile' },
    { id: 'settings', label: 'Settings', Icon: Settings, path: '/settings' }
  ]

  return (
    <aside className="bg-white min-h-screen p-4 w-64 flex-shrink-0">
      <div className="mb-8 px-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg flex items-center justify-center bg-primary-accent">
            <Calendar size={20} className="text-white" />
          </div>
          <span className="text-[18px] font-semibold text-gray-900">Healthcare Hub</span>
        </div>
      </div>

      <nav className="space-y-1">
        {navItems.map((item) => {
          const isActiveItem = activeItem === item.id
          const IconComponent = item.Icon
          
          return (
            <Link
              key={item.id}
              to={item.path}
              onClick={() => onNavigate(item.id)}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl text-[14px] font-medium transition-colors duration-200 ${
                isActiveItem
                  ? 'bg-gray-100 text-primary-dark'
                  : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
              }`}
            >
              <IconComponent 
                size={18}
                className={isActiveItem ? 'text-primary-dark' : 'text-gray-500'}
              />
              {item.label}
            </Link>
          )
        })}
      </nav>
    </aside>
  )
}
