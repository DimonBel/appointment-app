import React, { useState } from 'react'
import { Header } from '../components/layout/Header'
import { Sidebar } from '../components/layout/Sidebar'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Button } from '../components/ui/Button'
import { Icon } from '../components/ui/Icon'
import { mockAppointments } from '../utils/mockData'

export const DoctorList = () => {
  const [menuOpen, setMenuOpen] = useState(false)

  const handleNavigate = (itemId) => {
    console.log('Navigate to:', itemId)
    setMenuOpen(false)
  }

  return (
    <div className="min-h-screen bg-[#F2F2F2] flex">
      <Sidebar 
        activeItem="doctors" 
        onNavigate={handleNavigate}
      />
      
      <div className="flex-1 flex flex-col min-h-screen">
        <Header 
          onMenuClick={() => setMenuOpen(!menuOpen)}
        />
        
        <MainContent>
          <SectionHeader 
            title="Find Doctors"
            subtitle="Browse and book appointments with top healthcare professionals"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {mockAppointments.slice(0, 3).map((doctor) => (
              <div key={doctor.id} className="bg-white rounded-2xl shadow-sm p-6 hover:shadow-md transition-shadow">
                <div className="flex items-start gap-4 mb-4">
                  <div className="w-16 h-16 rounded-full bg-gray-200 overflow-hidden flex-shrink-0">
                    <img 
                      src={doctor.avatar} 
                      alt={doctor.doctorName}
                      className="w-full h-full object-cover"
                    />
                  </div>
                  <div className="flex-1">
                    <h3 className="text-[16px] font-semibold text-gray-900">{doctor.doctorName}</h3>
                    <p className="text-[14px] text-gray-600">{doctor.specialty}</p>
                  </div>
                </div>
                
                <div className="space-y-2 mb-4">
                  <div className="flex items-center gap-2 text-[14px] text-gray-600">
                    <Icon name="mapPin" size={16} />
                    {doctor.location}
                  </div>
                  <div className="flex items-center gap-2 text-[14px] text-gray-600">
                    <Icon name="star" size={16} />
                    <span className="font-medium">4.8</span>
                    <span className="text-gray-500">({doctor.reviews} reviews)</span>
                  </div>
                </div>

                <Button 
                  variant="primary" 
                  size="medium"
                  className="w-full"
                >
                  Book Appointment
                </Button>
              </div>
            ))}
          </div>
        </MainContent>
      </div>
    </div>
  )
}

export default DoctorList
