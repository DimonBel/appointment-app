import React from 'react'
import { Icon } from '../ui/Icon'
import { TABS } from '../../utils/constants'

export const BookingTabs = ({ activeTab, onTabChange, className = '' }) => {
  const tabs = [
    { id: TABS.UPCOMING, label: 'Upcoming' },
    { id: TABS.COMPLETED, label: 'Completed' },
    { id: TABS.CANCELED, label: 'Canceled' }
  ]

  return (
    <div className={`flex items-center gap-8 ${className}`}>
      {tabs.map((tab) => (
        <button
          key={tab.id}
          onClick={() => onTabChange(tab.id)}
          className={`relative pb-2 text-[14px] font-medium transition-colors duration-200 ${
            activeTab === tab.id
              ? 'text-[#1E2A38]'
              : 'text-gray-500 hover:text-gray-700'
          }`}
        >
          {tab.label}
          {activeTab === tab.id && (
            <div className="absolute bottom-0 left-0 right-0 h-[2px] bg-[#1E2A38]" />
          )}
        </button>
      ))}
    </div>
  )
}
