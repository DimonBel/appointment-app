import React from 'react'
import { Avatar } from '../ui/Avatar'
import { Icon } from '../ui/Icon'
import { Button } from '../ui/Button'
import { mockUser } from '../../utils/mockData'

export const Header = ({ onMenuClick, className = '' }) => {
  return (
    <header className={`${className} bg-[#1E2A38] text-white px-6 py-4 flex items-center justify-between sticky top-0 z-50`}>
      <div className="flex items-center gap-4">
        <button
          onClick={onMenuClick}
          className="p-2 hover:bg-white/10 rounded-lg transition-colors duration-200"
        >
          <Icon name="menu" size={20} />
        </button>
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-lg flex items-center justify-center" style={{ backgroundColor: '#4DA3FF' }}>
            <Icon name="calendar" size={20} color="#FFFFFF" />
          </div>
          <span className="text-[18px] font-semibold">Appointments</span>
        </div>
      </div>

      <div className="flex items-center gap-4">
        <button className="relative p-2 hover:bg-white/10 rounded-lg transition-colors duration-200">
          <Icon name="bell" size={20} />
          {mockUser.notifications > 0 && (
            <span 
              className="absolute top-1 right-1 w-2 h-2 rounded-full bg-[#4DA3FF]"
              style={{ backgroundColor: '#4DA3FF' }}
            />
          )}
        </button>
        
        <div className="flex items-center gap-3 ml-4 pl-4 border-l border-white/20">
          <Avatar 
            src={mockUser.avatar} 
            alt={mockUser.name} 
            size={32}
          />
          <div className="hidden sm:block">
            <p className="text-[14px] font-medium">{mockUser.name}</p>
            <p className="text-[12px] text-gray-300">{mockUser.email}</p>
          </div>
        </div>
      </div>
    </header>
  )
}
