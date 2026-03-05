import React from 'react'
import { Link } from 'react-router-dom'
import { useSelector } from 'react-redux'
import { Calendar, Users, MessageCircle, User, Settings, Stethoscope, Bell, Shield, Briefcase, Bot, Users2, ChevronRight, ChevronLeft } from 'lucide-react'

export const Sidebar = ({ activeItem, onNavigate, isOpen, onClose, onToggle }) => {
  const user = useSelector((state) => state.auth.user)
  const unreadCount = useSelector((state) => state.notifications?.unreadCount || 0)
  const isProfessional = user?.roles?.includes('Professional') || user?.roles?.includes('Doctor') || false
  const isAdmin = user?.roles?.includes('Admin') || false
  const isManagement = user?.roles?.includes('Management') || false

  const navItems = [
    { id: 'bookings', label: 'My Bookings', Icon: Calendar, path: '/bookings' },
    { id: 'doctors', label: 'Find Doctors', Icon: Users, path: '/doctors' },
    { id: 'chat', label: 'Messages', Icon: MessageCircle, path: '/chat' },
    { id: 'notifications', label: 'Notifications', Icon: Bell, path: '/notifications', badge: unreadCount > 0 ? unreadCount : null },
    { id: 'ai-assistant', label: 'AI Assistant', Icon: Bot, path: '/ai-assistant' },
    { id: 'profile', label: 'Profile', Icon: User, path: '/profile' },
    { id: 'client-panel', label: 'My Schedule', Icon: Users2, path: '/client-panel' },
    ...(isProfessional ? [{ id: 'doctor-profile', label: 'Professional Profile', Icon: Stethoscope, path: '/doctor-profile' }] : []),
    ...(isProfessional ? [{ id: 'doctor-panel', label: 'Doctor Panel', Icon: Users2, path: '/doctor-panel' }] : []),
    ...(isManagement || isAdmin ? [{ id: 'management', label: 'Management Panel', Icon: Briefcase, path: '/management' }] : []),
    ...(isAdmin ? [{ id: 'admin', label: 'Admin Panel', Icon: Shield, path: '/admin' }] : []),
    { id: 'settings', label: 'Settings', Icon: Settings, path: '/settings' }
  ]

  return (
    <>
      <button
        onClick={onToggle}
        className={`fixed top-[55%] -translate-y-1/2 z-50 p-2 bg-white text-gray-700 border border-gray-200 rounded-r-lg shadow-lg hover:bg-gray-50 transition-all duration-200 ${isOpen ? 'left-80' : 'left-0'}`}
        title={isOpen ? 'Close menu' : 'Open menu'}
      >
        {isOpen ? <ChevronLeft size={20} /> : <ChevronRight size={20} />}
      </button>

      {isOpen && (
        <>
          {/* Backdrop */}
          <div
            className="fixed top-16 left-0 right-0 bottom-0 z-40 bg-transparent"
            onClick={onClose}
          />

          {/* Dropdown Menu */}
          <div className="fixed top-16 left-0 z-40 bg-white shadow-2xl border border-gray-200 rounded-br-lg w-80 max-w-[90vw] h-[calc(100vh-64px)] overflow-y-auto">
            <div className="p-4 border-b border-gray-100">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg flex items-center justify-center bg-primary-accent flex-shrink-0">
                  <Calendar size={20} className="text-white" />
                </div>
                <span className="text-[18px] font-semibold text-gray-900">Booking Hub</span>
              </div>
            </div>

            <nav className="p-2 space-y-1">
              {navItems.map((item) => {
                const isActiveItem = activeItem === item.id
                const IconComponent = item.Icon

                return (
                  <Link
                    key={item.id}
                    to={item.path}
                    onClick={() => {
                      onNavigate(item.id)
                      onClose()
                    }}
                    className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl text-[14px] font-medium transition-colors duration-200 ${isActiveItem
                      ? 'bg-primary-accent/10 text-primary-dark'
                      : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
                      }`}
                  >
                    <IconComponent
                      size={18}
                      className={isActiveItem ? 'text-primary-dark' : 'text-gray-500'}
                    />
                    <span className="flex-1">{item.label}</span>
                    {item.badge && (
                      <span className="bg-primary-accent text-white text-xs px-2 py-0.5 rounded-full min-w-[20px] text-center font-semibold">
                        {item.badge}
                      </span>
                    )}
                  </Link>
                )
              })}
            </nav>
          </div>
        </>
      )}
    </>
  )
}
