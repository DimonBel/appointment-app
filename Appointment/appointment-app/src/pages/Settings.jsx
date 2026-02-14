import React, { useState } from 'react'
import { Header } from '../components/layout/Header'
import { Sidebar } from '../components/layout/Sidebar'
import { MainContent } from '../components/layout/MainContent'
import { Section, SectionHeader } from '../components/layout/MainContent'
import { Button } from '../components/ui/Button'
import { Icon } from '../components/ui/Icon'
import { mockUser } from '../utils/mockData'

export const Settings = () => {
  const [menuOpen, setMenuOpen] = useState(false)

  const handleNavigate = (itemId) => {
    console.log('Navigate to:', itemId)
    setMenuOpen(false)
  }

  const settingsSections = [
    {
      title: 'General',
      items: [
        { id: 'profile', name: 'Profile Information', icon: 'user', description: 'Update your personal details' },
        { id: 'security', name: 'Security & Password', icon: 'lock', description: 'Manage your security settings' },
        { id: 'notifications', name: 'Notifications', icon: 'bell', description: 'Configure notification preferences' }
      ]
    },
    {
      title: 'Preferences',
      items: [
        { id: 'language', name: 'Language', icon: 'globe', description: 'Select your preferred language' },
        { id: 'theme', name: 'Theme', icon: 'moon', description: 'Choose light or dark theme' },
        { id: 'privacy', name: 'Privacy', icon: 'eye', description: 'Control your privacy settings' }
      ]
    },
    {
      title: 'Support',
      items: [
        { id: 'help', name: 'Help Center', icon: 'help-circle', description: 'Get help and support' },
        { id: 'terms', name: 'Terms & Conditions', icon: 'file-text', description: 'View our terms and conditions' },
        { id: 'privacy', name: 'Privacy Policy', icon: 'shield', description: 'Learn about our privacy policy' }
      ]
    }
  ]

  return (
    <div className="min-h-screen bg-[#F2F2F2] flex">
      <Sidebar 
        activeItem="settings" 
        onNavigate={handleNavigate}
      />
      
      <div className="flex-1 flex flex-col min-h-screen">
        <Header 
          onMenuClick={() => setMenuOpen(!menuOpen)}
        />
        
        <MainContent>
          <SectionHeader 
            title="Settings"
            subtitle="Manage your account and app preferences"
          />

          <div className="bg-white rounded-2xl shadow-sm p-6 mb-6">
            <div className="flex items-center gap-4">
              <div className="w-16 h-16 rounded-full bg-gray-200 overflow-hidden flex-shrink-0">
                <img 
                  src={mockUser.avatar} 
                  alt={mockUser.name}
                  className="w-full h-full object-cover"
                />
              </div>
              <div className="flex-1">
                <h2 className="text-[24px] font-semibold text-gray-900 mb-1">{mockUser.name}</h2>
                <p className="text-[14px] text-gray-500">{mockUser.email}</p>
              </div>
              <Button variant="primary" size="medium">
                <Icon name="edit3" size={16} className="mr-2" />
                Edit Profile
              </Button>
            </div>
          </div>

          <div className="space-y-6">
            {settingsSections.map((section, sectionIndex) => (
              <div key={sectionIndex} className="bg-white rounded-2xl shadow-sm">
                <div className="p-4 border-b border-[#EAEAEA]">
                  <h3 className="text-[16px] font-semibold text-gray-900">{section.title}</h3>
                </div>
                <div className="divide-y divide-[#EAEAEA]">
                  {section.items.map((item) => (
                    <div 
                      key={item.id}
                      className="flex items-center justify-between p-4 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-10 h-10 rounded-lg bg-gray-100 flex items-center justify-center">
                          <Icon name={item.icon} size={20} color="#6B7280" />
                        </div>
                        <div>
                          <p className="text-[14px] font-medium text-gray-900">{item.name}</p>
                          <p className="text-[12px] text-gray-500">{item.description}</p>
                        </div>
                      </div>
                      <Icon name="chevron-right" size={16} color="#9CA3AF" />
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>

          <div className="bg-white rounded-2xl shadow-sm p-6 mt-6">
            <div className="flex items-center justify-between p-4 border-b border-[#EAEAEA]">
              <div className="flex items-center gap-4">
                <Icon name="log-out" size={20} color="#DC2626" />
                <div>
                  <p className="text-[14px] font-medium text-red-600">Log Out</p>
                  <p className="text-[12px] text-gray-500">Sign out of your account</p>
                </div>
              </div>
              <Button variant="secondary" size="small">
                Log Out
              </Button>
            </div>
          </div>
        </MainContent>
      </div>
    </div>
  )
}

export default Settings
