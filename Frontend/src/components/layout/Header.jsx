import React from 'react'
import { useSelector, useDispatch } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { Menu, Bell, Calendar } from 'lucide-react'
import { logout } from '../../store/slices/authSlice'

export const Header = ({ onMenuClick, className = '' }) => {
  const user = useSelector((state) => state.auth.user)
  const notifications = useSelector((state) => state.ui.notifications)
  const dispatch = useDispatch()
  const navigate = useNavigate()

  const handleLogout = () => {
    dispatch(logout())
    navigate('/login')
  }

  return (
    <header className={`${className} bg-primary-dark text-white px-6 py-4 flex items-center justify-between sticky top-0 z-50`}>
      <div className="flex items-center gap-4">
        <button
          onClick={onMenuClick}
          className="p-2 hover:bg-white/10 rounded-lg transition-colors duration-200"
        >
          <Menu size={20} />
        </button>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg flex items-center justify-center bg-primary-accent">
            <Calendar size={20} className="text-white" />
          </div>
          <span className="text-[18px] font-semibold">Healthcare Hub</span>
        </div>
      </div>

      <div className="flex items-center gap-4">
        <button className="relative p-2 hover:bg-white/10 rounded-lg transition-colors duration-200">
          <Bell size={20} />
          {notifications.length > 0 && (
            <span className="absolute top-1 right-1 w-2 h-2 rounded-full bg-primary-accent" />
          )}
        </button>
        
        <div className="flex items-center gap-3 ml-4 pl-4 border-l border-white/20">
          <div className="w-8 h-8 rounded-full bg-primary-accent flex items-center justify-center text-sm font-semibold">
            {user?.firstName?.[0]}{user?.lastName?.[0]}
          </div>
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
  )
}
