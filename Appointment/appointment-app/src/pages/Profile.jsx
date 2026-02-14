import React, { useState } from 'react'
import { Header } from '../components/layout/Header'
import { Sidebar } from '../components/layout/Sidebar'
import { MainContent } from '../components/layout/MainContent'
import { Section, SectionHeader } from '../components/layout/MainContent'
import { Avatar, AvatarGroup } from '../components/ui/Avatar'
import { Button } from '../components/ui/Button'
import { Icon } from '../components/ui/Icon'
import { ButtonGroup } from '../components/ui/Button'
import { mockUser } from '../utils/mockData'
import { TABS } from '../utils/constants'

export const Profile = () => {
  const [activeItem, setActiveItem] = useState('edit-profile')
  const [menuOpen, setMenuOpen] = useState(false)

  const handleNavigate = (itemId) => {
    setActiveItem(itemId)
    setMenuOpen(false)
  }

  const recentDoctors = [
    { name: 'Dr. Sarah Johnson', specialty: 'Cardiologist', avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Sarah' },
    { name: 'Dr. Michael Chen', specialty: 'Dermatologist', avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Michael' },
    { name: 'Dr. Emily Davis', specialty: 'Pediatrician', avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Emily' },
    { name: 'Dr. James Wilson', specialty: 'Orthopedic Surgeon', avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=James' },
  ]

  return (
    <div className="min-h-screen bg-[#F2F2F2] flex">
      <Sidebar 
        activeItem={activeItem} 
        onNavigate={handleNavigate}
      />
      
      <div className="flex-1 flex flex-col min-h-screen">
        <Header 
          onMenuClick={() => setMenuOpen(!menuOpen)}
        />
        
        <MainContent>
          <SectionHeader 
            title="My Profile"
            subtitle="Manage your account and preferences"
          />

          <div className="bg-white rounded-2xl shadow-sm p-6 mb-6">
            <div className="flex flex-col sm:flex-row sm:items-center gap-6">
              <Avatar 
                src={mockUser.avatar} 
                alt={mockUser.name} 
                size={80}
              />
              <div className="flex-1">
                <h2 className="text-[24px] font-semibold text-gray-900 mb-2">{mockUser.name}</h2>
                <p className="text-[14px] text-gray-500 mb-1">{mockUser.email}</p>
                <p className="text-[14px] text-gray-500">{mockUser.phone}</p>
              </div>
              <Button variant="primary" size="medium">
                <Icon name="edit3" size={16} className="mr-2" />
                Edit Profile
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Section>
              <div className="bg-white rounded-2xl shadow-sm p-6">
                <h3 className="text-[16px] font-semibold text-gray-900 mb-4">Account Settings</h3>
                <div className="space-y-4">
                  <div className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors">
                    <div className="flex items-center gap-3">
                      <Icon name="user" size={20} color="#6B7280" />
                      <span className="text-[14px] text-gray-700">Personal Information</span>
                    </div>
                    <Icon name="chevron-right" size={16} color="#9CA3AF" />
                  </div>
                  <div className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors">
                    <div className="flex items-center gap-3">
                      <Icon name="bell" size={20} color="#6B7280" />
                      <span className="text-[14px] text-gray-700">Notifications</span>
                    </div>
                    <Icon name="chevron-right" size={16} color="#9CA3AF" />
                  </div>
                  <div className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors">
                    <div className="flex items-center gap-3">
                      <Icon name="lock" size={20} color="#6B7280" />
                      <span className="text-[14px] text-gray-700">Privacy & Security</span>
                    </div>
                    <Icon name="chevron-right" size={16} color="#9CA3AF" />
                  </div>
                  <div className="flex items-center justify-between p-3 hover:bg-gray-50 rounded-lg transition-colors">
                    <div className="flex items-center gap-3">
                      <Icon name="shield" size={20} color="#6B7280" />
                      <span className="text-[14px] text-gray-700">Health Records</span>
                    </div>
                    <Icon name="chevron-right" size={16} color="#9CA3AF" />
                  </div>
                </div>
              </div>
            </Section>

            <Section>
              <div className="bg-white rounded-2xl shadow-sm p-6">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-[16px] font-semibold text-gray-900">Favorite Doctors</h3>
                  <Button variant="ghost" size="small">
                    View All
                  </Button>
                </div>
                <div className="flex flex-wrap gap-3">
                  {recentDoctors.map((doctor, index) => (
                    <div key={index} className="flex items-center gap-2 px-3 py-2 bg-gray-50 rounded-lg">
                      <Avatar src={doctor.avatar} alt={doctor.name} size={32} />
                      <div>
                        <p className="text-[14px] font-medium text-gray-900 truncate" style={{ maxWidth: '100px' }}>
                          {doctor.name}
                        </p>
                        <p className="text-[12px] text-gray-500">{doctor.specialty}</p>
                      </div>
                    </div>
                  ))}
                  <Button variant="ghost" size="small" className="px-3 py-2">
                    <Icon name="plus" size={14} />
                  </Button>
                </div>
              </div>
            </Section>

            <Section>
              <div className="bg-white rounded-2xl shadow-sm p-6">
                <h3 className="text-[16px] font-semibold text-gray-900 mb-4">Recent Activity</h3>
                <div className="space-y-4">
                  <div className="flex gap-3">
                    <div className="w-8 h-8 rounded-full bg-green-100 flex items-center justify-center flex-shrink-0">
                      <Icon name="check" size={16} color="#16A34A" />
                    </div>
                    <div>
                      <p className="text-[14px] text-gray-900">Appointment completed</p>
                      <p className="text-[12px] text-gray-500">2 days ago</p>
                    </div>
                  </div>
                  <div className="flex gap-3">
                    <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0">
                      <Icon name="calendar" size={16} color="#2563EB" />
                    </div>
                    <div>
                      <p className="text-[14px] text-gray-900">New appointment booked</p>
                      <p className="text-[12px] text-gray-500">1 week ago</p>
                    </div>
                  </div>
                  <div className="flex gap-3">
                    <div className="w-8 h-8 rounded-full bg-purple-100 flex items-center justify-center flex-shrink-0">
                      <Icon name="heart" size={16} color="#9333EA" />
                    </div>
                    <div>
                      <p className="text-[14px] text-gray-900">Doctor favorited</p>
                      <p className="text-[12px] text-gray-500">2 weeks ago</p>
                    </div>
                  </div>
                </div>
              </div>
            </Section>

            <Section>
              <div className="bg-white rounded-2xl shadow-sm p-6">
                <h3 className="text-[16px] font-semibold text-gray-900 mb-4">Quick Actions</h3>
                <div className="grid grid-cols-2 gap-3">
                  <Button variant="secondary" size="small" className="w-full">
                    <Icon name="message-square" size={16} className="mr-2" />
                    Contact Support
                  </Button>
                  <Button variant="secondary" size="small" className="w-full">
                    <Icon name="help-circle" size={16} className="mr-2" />
                    Help Center
                  </Button>
                </div>
              </div>
            </Section>
          </div>
        </MainContent>
      </div>
    </div>
  )
}

export default Profile
