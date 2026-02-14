import React, { useState, useEffect } from 'react'
import { Header } from '../components/layout/Header'
import { Sidebar } from '../components/layout/Sidebar'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Button } from '../components/ui/Button'
import { Icon } from '../components/ui/Icon'
import { BookingModal } from '../components/booking/BookingModal'
import { professionalsAPI, ordersAPI } from '../services/api'

export const DoctorList = () => {
  const [menuOpen, setMenuOpen] = useState(false)
  const [professionals, setProfessionals] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [bookingModalOpen, setBookingModalOpen] = useState(false)
  const [selectedProfessional, setSelectedProfessional] = useState(null)

  useEffect(() => {
    const fetchProfessionals = async () => {
      try {
        setLoading(true)
        setError(null)
        const response = await professionalsAPI.getAll(true, 1, 20)
        setProfessionals(response)
      } catch (err) {
        console.error('Error fetching professionals:', err)
        setError(err.message)
        setProfessionals([])
      } finally {
        setLoading(false)
      }
    }

    fetchProfessionals()
  }, [])

  const handleBookAppointment = (professional) => {
    setSelectedProfessional(professional)
    setBookingModalOpen(true)
  }

  const handleConfirmBooking = async (orderData) => {
    try {
      await ordersAPI.create(orderData)
      
      const appointmentDate = new Date(orderData.scheduledDateTime)
      const dayName = appointmentDate.toLocaleDateString('en-US', { weekday: 'long' })
      const dateStr = appointmentDate.toLocaleDateString()
      const timeStr = appointmentDate.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      
      alert(`Appointment booked successfully!\n${dayName}, ${dateStr} at ${timeStr}`)
      setBookingModalOpen(false)
      setSelectedProfessional(null)
    } catch (err) {
      console.error('Error booking appointment:', err)
      alert('Failed to book appointment: ' + err.message)
    }
  }

  const handleCloseModal = () => {
    setBookingModalOpen(false)
    setSelectedProfessional(null)
  }

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

          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded relative mb-4">
              <strong className="font-bold">Error: </strong>
              <span className="block sm:inline">{error}</span>
            </div>
          )}

          {loading ? (
            <div className="text-center py-12">
              <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-[#1E2A38]"></div>
              <p className="mt-4 text-gray-600">Loading professionals...</p>
            </div>
          ) : professionals.length === 0 ? (
            <div className="text-center py-12">
              <p className="text-gray-600">No professionals available at the moment.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {professionals.map((professional) => (
                <div key={professional.id} className="bg-white rounded-2xl shadow-sm p-6 hover:shadow-md transition-shadow">
                  <div className="flex items-start gap-4 mb-4">
                    <div className="w-16 h-16 rounded-full bg-gray-200 overflow-hidden flex-shrink-0">
                      <img 
                        src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${professional.id}`}
                        alt={professional.title}
                        className="w-full h-full object-cover"
                      />
                    </div>
                    <div className="flex-1">
                      <h3 className="text-[16px] font-semibold text-gray-900">{professional.title || 'Professional'}</h3>
                      <p className="text-[14px] text-gray-600">{professional.specialization || 'General Practice'}</p>
                      {professional.experienceYears && (
                        <p className="text-[12px] text-gray-500 mt-1">{professional.experienceYears} years experience</p>
                      )}
                    </div>
                  </div>
                  
                  <div className="space-y-2 mb-4">
                    {professional.qualifications && (
                      <div className="flex items-start gap-2 text-[14px] text-gray-600">
                        <Icon name="award" size={16} className="mt-0.5" />
                        <span className="flex-1">{professional.qualifications}</span>
                      </div>
                    )}
                    {professional.hourlyRate && (
                      <div className="flex items-center gap-2 text-[14px] text-gray-600">
                        <Icon name="dollarSign" size={16} />
                        <span className="font-medium">${professional.hourlyRate}/hour</span>
                      </div>
                    )}
                    <div className="flex items-center gap-2 text-[14px] text-gray-600">
                      <Icon name="star" size={16} />
                      <span className="font-medium">4.8</span>
                      <span className="text-gray-500">(120 reviews)</span>
                    </div>
                  </div>

                  <Button 
                    variant="primary" 
                    size="medium"
                    className="w-full"
                    onClick={() => handleBookAppointment(professional)}
                    disabled={!professional.isAvailable}
                  >
                    {professional.isAvailable ? 'Book Appointment' : 'Unavailable'}
                  </Button>
                </div>
              ))}
            </div>
          )}
        </MainContent>
      </div>

      {/* Booking Modal */}
      <BookingModal 
        isOpen={bookingModalOpen}
        onClose={handleCloseModal}
        professional={selectedProfessional}
        onConfirm={handleConfirmBooking}
      />
    </div>
  )
}

export default DoctorList
