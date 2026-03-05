import React, { useState } from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { Bell, House } from 'lucide-react'
import { logout } from '../../store/slices/authSlice'
import { Avatar } from '../ui/Avatar'
import { Sidebar } from './Sidebar'

export const Header = ({ activeItem, onNavigate, className = '' }) => {
  const user = useSelector((state) => state.auth.user)
  const isAuthenticated = useSelector((state) => state.auth.isAuthenticated)
  const unreadCount = useSelector((state) => state.notifications?.unreadCount || 0)
  const dispatch = useDispatch()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)

  const handleLogout = () => {
    dispatch(logout())
    navigate('/')
  }

  const handleHomeClick = () => {
    const isProfessional = user?.roles?.includes('Professional') || user?.roles?.includes('Doctor') || false
    navigate(isAuthenticated ? (isProfessional ? '/doctor-panel' : '/bookings') : '/')
  }

  const toggleMenu = () => {
    setMenuOpen(!menuOpen)
  }

  const closeMenu = () => {
    setMenuOpen(false)
  }

  return (
    <>
      <header className={`${className} bg-primary-dark text-white px-6 py-4 flex items-center justify-between sticky top-0 z-50`}>
        <div className="flex items-center gap-4">
          <button
            onClick={handleHomeClick}
            className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-white/10 transition-colors duration-200"
            title="Go Home"
          >
            <House size={18} />
            <span className="text-[14px] font-medium">Home</span>
          </button>
        </div>

        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/notifications')}
            className="relative p-2 hover:bg-white/10 rounded-lg transition-colors duration-200"
          >
            <Bell size={20} />
            {unreadCount > 0 && (
              <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] flex items-center justify-center rounded-full bg-primary-accent text-white text-[10px] font-bold px-1">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </button>

          <div className="flex items-center gap-3 ml-4 pl-4 border-l border-white/20">
            <Avatar
              src={user?.avatarUrl}
              alt={`${user?.firstName || ''} ${user?.lastName || ''}`.trim()}
              size={32}
              className="ring-1 ring-white/20"
            />
            <div className="hidden sm:block">
              <p className="text-[14px] font-medium">{user?.firstName} {user?.lastName}</p>
              <p className="text-[12px] text-gray-300">{user?.email}</p>
            </div>
            <button
              onClick={handleLogout}
              className="ml-2 px-3 py-1 text-xs rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Dropdown Sidebar */}
      <Sidebar
        activeItem={activeItem}
        onNavigate={onNavigate}
        isOpen={menuOpen}
        onClose={closeMenu}
        onToggle={toggleMenu}
      />
    </>
  )
}
