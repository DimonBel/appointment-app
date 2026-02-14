import React, { useState, useEffect } from 'react'
import { BookingTabs } from '../components/booking/BookingTabs'
import { BookingList } from '../components/booking/BookingList'
import { RescheduleModal } from '../components/booking/RescheduleModal'
import { Sidebar } from '../components/layout/Sidebar'
import { Header } from '../components/layout/Header'
import { MainContent, Section, SectionHeader } from '../components/layout/MainContent'
import { ordersAPI } from '../services/api'

export const Bookings = () => {
  const [activeTab, setActiveTab] = useState('upcoming')
  const [menuOpen, setMenuOpen] = useState(false)
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [rescheduleModalOpen, setRescheduleModalOpen] = useState(false)
  const [selectedAppointment, setSelectedAppointment] = useState(null)

  // Test client ID from seeded database (client@appointment.com)
  const testClientId = '019c5b2e-9461-78f1-9552-b0a90accd7a8'

  // Fetch appointments from backend
  useEffect(() => {
    const fetchAppointments = async () => {
      try {
        setLoading(true)
        setError(null)
        
        let response
        
        if (activeTab === 'upcoming') {
          // For upcoming, fetch all statuses and filter to show Requested (0) and Approved (1)
          response = await ordersAPI.getByClient(testClientId, null)
          response = response.filter(order => 
            order.status === 0 || order.status === 1 // Requested or Approved
          )
        } else {
          // For other tabs, fetch specific status
          const statusMap = {
            'completed': 4, // Completed
            'canceled': 3   // Cancelled
          }
          const status = statusMap[activeTab]
          response = await ordersAPI.getByClient(testClientId, status)
        }
        
        // Transform backend data to match frontend format
        const transformedData = response.map(order => {
          const professionalName = order.professional 
            ? `${order.professional.firstName || ''} ${order.professional.lastName || ''}`.trim()
            : `Professional ${order.professionalId.substring(0, 8)}`
          
          // Use professionalEntity ID for avatar to match doctor list
          const avatarSeed = order.professionalEntity?.id || order.professionalId
          
          return {
            id: order.id,
            doctorId: order.professionalId,
            doctorName: professionalName,
            specialty: order.title || 'General',
            clinic: 'Appointment',
            location: order.description || 'To be confirmed',
            date: new Date(order.scheduledDateTime).toLocaleDateString(),
            time: new Date(order.scheduledDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
            scheduledDateTime: order.scheduledDateTime, // Keep raw date for sorting
            status: activeTab,
            avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${avatarSeed}`
          }
        })
        
        // Sort by date (soonest first)
        transformedData.sort((a, b) => {
          return new Date(a.scheduledDateTime) - new Date(b.scheduledDateTime)
        })
        
        setAppointments(transformedData)
      } catch (err) {
        console.error('Error fetching appointments:', err)
        setError(err.message)
        setAppointments([])
      } finally {
        setLoading(false)
      }
    }

    fetchAppointments()
  }, [activeTab])

  const handleReschedule = (appointment) => {
    setSelectedAppointment(appointment)
    setRescheduleModalOpen(true)
  }

  const handleConfirmReschedule = async (rescheduleData) => {
    try {
      await ordersAPI.reschedule(rescheduleData.appointmentId, {
        newScheduledDateTime: rescheduleData.newDateTime,
        notes: rescheduleData.notes
      })
      
      alert('Appointment rescheduled successfully!')
      setRescheduleModalOpen(false)
      setSelectedAppointment(null)
      
      // Refresh the appointments list
      let response
      
      if (activeTab === 'upcoming') {
        response = await ordersAPI.getByClient(testClientId, null)
        response = response.filter(order => 
          order.status === 0 || order.status === 1
        )
      } else {
        const statusMap = {
          'completed': 4,
          'canceled': 3
        }
        const status = statusMap[activeTab]
        response = await ordersAPI.getByClient(testClientId, status)
      }
      
      const transformedData = response.map(order => {
        const professionalName = order.professional 
          ? `${order.professional.firstName || ''} ${order.professional.lastName || ''}`.trim()
          : `Professional ${order.professionalId.substring(0, 8)}`
        
        const avatarSeed = order.professionalEntity?.id || order.professionalId
        
        return {
          id: order.id,
          doctorId: order.professionalId,
          doctorName: professionalName,
          specialty: order.title || 'General',
          clinic: 'Appointment',
          location: order.description || 'To be confirmed',
          date: new Date(order.scheduledDateTime).toLocaleDateString(),
          time: new Date(order.scheduledDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          status: activeTab,
          avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${avatarSeed}`,
          scheduledDateTime: order.scheduledDateTime
        }
      })
      
      // Sort by scheduled date/time (soonest first)
      transformedData.sort((a, b) => {
        return new Date(a.scheduledDateTime) - new Date(b.scheduledDateTime)
      })
      
      setAppointments(transformedData)
    } catch (err) {
      console.error('Error rescheduling appointment:', err)
      alert('Failed to reschedule appointment: ' + err.message)
    }
  }

  const handleCloseRescheduleModal = () => {
    setRescheduleModalOpen(false)
    setSelectedAppointment(null)
  }

  const handleCancel = async (appointment) => {
    if (!confirm('Are you sure you want to cancel this appointment?')) {
      return
    }
    
    try {
      await ordersAPI.cancel(appointment.id)
      alert('Appointment cancelled successfully!')
      
      // Refetch to ensure UI matches backend
      let response
      
      if (activeTab === 'upcoming') {
        response = await ordersAPI.getByClient(testClientId, null)
        response = response.filter(order => 
          order.status === 0 || order.status === 1
        )
      } else {
        const statusMap = {
          'completed': 4,
          'canceled': 3
        }
        const status = statusMap[activeTab]
        response = await ordersAPI.getByClient(testClientId, status)
      }
      
      const transformedData = response.map(order => {
        const professionalName = order.professional 
          ? `${order.professional.firstName || ''} ${order.professional.lastName || ''}`.trim()
          : `Professional ${order.professionalId.substring(0, 8)}`
        
        const avatarSeed = order.professionalEntity?.id || order.professionalId
        
        return {
          id: order.id,
          doctorId: order.professionalId,
          doctorName: professionalName,
          specialty: order.title || 'General',
          clinic: 'Appointment',
          location: order.description || 'To be confirmed',
          date: new Date(order.scheduledDateTime).toLocaleDateString(),
          time: new Date(order.scheduledDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          scheduledDateTime: order.scheduledDateTime,
          status: activeTab,
          avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${avatarSeed}`
        }
      })
      
      transformedData.sort((a, b) => {
        return new Date(a.scheduledDateTime) - new Date(b.scheduledDateTime)
      })
      
      setAppointments(transformedData)
    } catch (err) {
      console.error('Error cancelling appointment:', err)
      alert('Failed to cancel appointment: ' + err.message)
    }
  }

  const handleComplete = async (appointment) => {
    if (!confirm('Mark this appointment as completed?')) {
      return
    }
    
    try {
      await ordersAPI.complete(appointment.id)
      alert('Appointment marked as completed!')
      
      // Refetch to ensure UI matches backend
      let response
      
      if (activeTab === 'upcoming') {
        response = await ordersAPI.getByClient(testClientId, null)
        response = response.filter(order => 
          order.status === 0 || order.status === 1
        )
      } else {
        const statusMap = {
          'completed': 4,
          'canceled': 3
        }
        const status = statusMap[activeTab]
        response = await ordersAPI.getByClient(testClientId, status)
      }
      
      const transformedData = response.map(order => {
        const professionalName = order.professional 
          ? `${order.professional.firstName || ''} ${order.professional.lastName || ''}`.trim()
          : `Professional ${order.professionalId.substring(0, 8)}`
        
        const avatarSeed = order.professionalEntity?.id || order.professionalId
        
        return {
          id: order.id,
          doctorId: order.professionalId,
          doctorName: professionalName,
          specialty: order.title || 'General',
          clinic: 'Appointment',
          location: order.description || 'To be confirmed',
          date: new Date(order.scheduledDateTime).toLocaleDateString(),
          time: new Date(order.scheduledDateTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
          scheduledDateTime: order.scheduledDateTime,
          status: activeTab,
          avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${avatarSeed}`
        }
      })
      
      transformedData.sort((a, b) => {
        return new Date(a.scheduledDateTime) - new Date(b.scheduledDateTime)
      })
      
      setAppointments(transformedData)
    } catch (err) {
      console.error('Error completing appointment:', err)
      alert('Failed to complete appointment: ' + err.message)
    }
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
            {error && (
              <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded relative mb-4">
                <strong className="font-bold">Error: </strong>
                <span className="block sm:inline">{error}</span>
              </div>
            )}
            
            <BookingList
              appointments={appointments}
              loading={loading}
              onViewDetails={handleReschedule}
              onReschedule={handleReschedule}
              onCancel={handleCancel}
              onComplete={handleComplete}
            />
          </Section>
        </MainContent>
      </div>

      {/* Reschedule Modal */}
      <RescheduleModal 
        isOpen={rescheduleModalOpen}
        onClose={handleCloseRescheduleModal}
        appointment={selectedAppointment}
        onConfirm={handleConfirmReschedule}
      />
    </div>
  )
}

export default Bookings
