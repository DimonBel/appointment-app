import React, { useState } from 'react'
import { BookingTabs } from '../components/booking/BookingTabs'
import { BookingList } from '../components/booking/BookingList'
import { Sidebar } from '../components/layout/Sidebar'
import { Header } from '../components/layout/Header'
import { MainContent, Section, SectionHeader } from '../components/layout/MainContent'
import { mockAppointments } from '../utils/mockData'

export const Bookings = () => {
  const [activeTab, setActiveTab] = useState('upcoming')
  const [menuOpen, setMenuOpen] = useState(false)

  const filteredAppointments = mockAppointments.filter(
    appointment => appointment.status === activeTab
  )

  const handleReschedule = (appointment) => {
    console.log('Reschedule appointment:', appointment)
  }

  const handleCancel = (appointment) => {
    console.log('Cancel appointment:', appointment)
  }

  const handleNavigate = (itemId) => {
    console.log('Navigate to:', itemId)
    setMenuOpen(false)
  }

  return (
    <div className="min-h-screen bg-[#F2F2F2] flex">
      <Sidebar 
        activeItem="bookings" 
        onNavigate={handleNavigate}
      />
      
      <div className="flex-1 flex flex-col min-h-screen">
        <Header 
          onMenuClick={() => setMenuOpen(!menuOpen)}
        />
        
        <MainContent>
          <SectionHeader 
            title="My Bookings"
            subtitle="Manage and track your appointments"
          />
          
          <Section>
            <BookingTabs 
              activeTab={activeTab}
              onTabChange={setActiveTab}
            />
          </Section>

          <Section>
            <BookingList
              appointments={filteredAppointments}
              loading={false}
              onViewDetails={handleReschedule}
              onReschedule={handleReschedule}
              onCancel={handleCancel}
            />
          </Section>
        </MainContent>
      </div>
    </div>
  )
}

export default Bookings
