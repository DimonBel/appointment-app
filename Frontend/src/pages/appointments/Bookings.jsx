import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardHeader, CardTitle, CardContent } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { StatusBadge } from '../../components/ui/Badge'
import { Avatar } from '../../components/ui/Avatar'
import { Loader } from '../../components/ui/Loader'
import FileUpload from '../../components/ui/FileUpload'
import { appointmentService } from '../../services/appointmentService'
import { userService } from '../../services/userService'
import documentService from '../../services/documentService'
import { Calendar, Clock, MapPin, Phone, Paperclip, X } from 'lucide-react'

export const Bookings = () => {
  const [activeTab, setActiveTab] = useState('upcoming')
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [actionLoadingId, setActionLoadingId] = useState(null)
  const [appointmentDocuments, setAppointmentDocuments] = useState({})
  const [showFileUploadForId, setShowFileUploadForId] = useState(null)
  const [loadingDocuments, setLoadingDocuments] = useState({})
  const token = useSelector((state) => state.auth.token)

  useEffect(() => {
    fetchAppointments()
  }, [activeTab, token])

  const fetchAppointmentDocuments = async (appointmentId) => {
    if (appointmentDocuments[appointmentId]) return

    setLoadingDocuments((prev) => ({ ...prev, [appointmentId]: true }))
    try {
      const docs = await documentService.getDocumentsByLinkedEntity('Order', appointmentId, {}, token)
      setAppointmentDocuments((prev) => ({ ...prev, [appointmentId]: Array.isArray(docs) ? docs : [] }))
    } catch (error) {
      console.error('Error fetching appointment documents:', error)
    } finally {
      setLoadingDocuments((prev) => ({ ...prev, [appointmentId]: false }))
    }
  }

  useEffect(() => {
    fetchAppointments()
  }, [activeTab, token])

  const fetchAppointments = async () => {
    setLoading(true)
    try {
      const data = await appointmentService.getOrders(token)
      const allOrders = Array.isArray(data) ? data : []

      const uniqueProfessionalUserIds = Array.from(
        new Set(
          allOrders
            .map((order) => order.professionalId)
            .filter(Boolean)
        )
      )

      const identityUsersById = {}
      await Promise.all(
        uniqueProfessionalUserIds.map(async (userId) => {
          try {
            const user = await userService.getUserById(userId, token)
            if (user?.id) {
              identityUsersById[user.id] = user
            }
          } catch {
            // Keep fallback data from order payload
          }
        })
      )

      const statusToUi = (status) => {
        switch (status) {
          case 0:
            return 'pending'
          case 1:
            return 'confirmed'
          case 3:
            return 'cancelled'
          case 4:
            return 'completed'
          default:
            return 'pending'
        }
      }

      const mapped = allOrders.map((order) => {
        const identityDoctor = order.professionalId
          ? identityUsersById[order.professionalId]
          : null

        const identityDoctorName = identityDoctor
          ? `${identityDoctor.firstName || ''} ${identityDoctor.lastName || ''}`.trim()
          : ''

        const professionalName = order.professional
          ? `${order.professional.firstName || ''} ${order.professional.lastName || ''}`.trim()
          : order.professionalId
            ? `Doctor ${String(order.professionalId).slice(0, 8)}`
            : 'Doctor'

        const scheduled = order.scheduledDateTime ? new Date(order.scheduledDateTime) : null

        return {
          id: order.id,
          orderStatus: order.status,
          doctorName: identityDoctorName || professionalName || 'Doctor',
          doctorAvatar: identityDoctor?.avatarUrl || null,
          specialty: order.title || 'General Consultation',
          location: order.description || 'To be confirmed',
          date: scheduled ? scheduled.toLocaleDateString() : '-',
          time: scheduled ? scheduled.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '-',
          status: statusToUi(order.status),
        }
      })

      const filtered = mapped.filter((apt) => {
        if (activeTab === 'upcoming') {
          return apt.orderStatus === 0 || apt.orderStatus === 1
        }
        if (activeTab === 'completed') {
          return apt.orderStatus === 4
        }
        return apt.orderStatus === 3
      })

      setAppointments(filtered)
    } catch (error) {
      console.error('Error fetching appointments:', error)
      setAppointments([])
    } finally {
      setLoading(false)
    }
  }

  const handleCancel = async (appointment) => {
    if (!appointment?.id) return
    if (!window.confirm('Cancel this appointment?')) return

    try {
      setActionLoadingId(appointment.id)
      await appointmentService.cancelOrder(appointment.id, token)
      await fetchAppointments()
    } catch (error) {
      console.error('Error cancelling appointment:', error)
      alert(error.response?.data?.message || 'Failed to cancel appointment')
    } finally {
      setActionLoadingId(null)
    }
  }

  const handleReschedule = async (appointment) => {
    if (!appointment?.id) return

    const dateInput = window.prompt('Enter new date and time (YYYY-MM-DDTHH:mm), e.g. 2026-02-20T14:30')
    if (!dateInput) return

    const parsed = new Date(dateInput)
    if (Number.isNaN(parsed.getTime())) {
      alert('Invalid date format')
      return
    }

    const notes = window.prompt('Reschedule note (optional)') || ''

    try {
      setActionLoadingId(appointment.id)
      await appointmentService.rescheduleOrder(appointment.id, parsed.toISOString(), notes, token)
      await fetchAppointments()
    } catch (error) {
      console.error('Error rescheduling appointment:', error)
      alert(error.response?.data?.message || 'Failed to reschedule appointment')
    } finally {
      setActionLoadingId(null)
    }
  }

  const handleComplete = async (appointment) => {
    if (!appointment?.id) return
    if (!window.confirm('Mark this appointment as completed?')) return

    const notes = window.prompt('Completion note (optional)') || ''

    try {
      setActionLoadingId(appointment.id)
      await appointmentService.completeOrder(appointment.id, notes, token)
      await fetchAppointments()
    } catch (error) {
      console.error('Error completing appointment:', error)
      alert(error.response?.data?.message || 'Failed to complete appointment')
    } finally {
      setActionLoadingId(null)
    }
  }

  const tabs = [
    { id: 'upcoming', label: 'Upcoming' },
    { id: 'completed', label: 'Completed' },
    { id: 'cancelled', label: 'Cancelled' },
  ]

  return (
    <MainContent>
      <SectionHeader 
        title="My Bookings"
        subtitle="Manage and track your appointments"
      />

      {/* Tabs */}
      <div className="flex gap-2 mb-6">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-2 rounded-lg font-medium transition-all ${
              activeTab === tab.id
                ? 'bg-primary-dark text-white'
                : 'bg-white text-text-secondary hover:bg-gray-50'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Appointments List */}
      {loading ? (
        <div className="flex justify-center py-12">
          <Loader size="lg" />
        </div>
      ) : appointments.length === 0 ? (
        <Card>
          <CardContent className="text-center py-12">
            <Calendar size={48} className="mx-auto text-text-muted mb-4" />
            <p className="text-text-secondary">No {activeTab} appointments</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {appointments.map((appointment) => (
            <Card key={appointment.id} hover className="transition-all">
              <CardContent className="flex items-start gap-4 p-6">
                <Avatar 
                  src={appointment.doctorAvatar}
                  alt={appointment.doctorName}
                  size={56}
                />
                
                <div className="flex-1">
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <h3 className="font-semibold text-text-primary text-lg">
                        {appointment.doctorName}
                      </h3>
                      <p className="text-text-secondary text-sm">{appointment.specialty}</p>
                    </div>
                    <StatusBadge status={appointment.status} />
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mt-4 text-sm">
                    <div className="flex items-center gap-2 text-text-secondary">
                      <Calendar size={16} />
                      <span>{appointment.date}</span>
                    </div>
                    <div className="flex items-center gap-2 text-text-secondary">
                      <Clock size={16} />
                      <span>{appointment.time}</span>
                    </div>
                    <div className="flex items-center gap-2 text-text-secondary">
                      <MapPin size={16} />
                      <span>{appointment.location}</span>
                    </div>
                  </div>

                  {activeTab === 'upcoming' && (
                    <>
                      <div className="flex gap-2 mt-4">
                        <Button size="sm" variant="primary" onClick={() => handleReschedule(appointment)} disabled={actionLoadingId === appointment.id}>
                          Reschedule
                        </Button>
                        <Button size="sm" variant="outline" onClick={() => handleCancel(appointment)} disabled={actionLoadingId === appointment.id}>
                          Cancel
                        </Button>
                        <Button size="sm" variant="ghost" onClick={() => handleComplete(appointment)} disabled={actionLoadingId === appointment.id}>
                          <Phone size={16} className="mr-1" />
                          Complete
                        </Button>
                        <Button 
                          size="sm" 
                          variant="ghost" 
                          onClick={() => {
                            setShowFileUploadForId(showFileUploadForId === appointment.id ? null : appointment.id)
                            if (!appointmentDocuments[appointment.id]) {
                              fetchAppointmentDocuments(appointment.id)
                            }
                          }}
                          className={showFileUploadForId === appointment.id ? 'bg-primary-accent text-white' : ''}
                        >
                          <Paperclip size={16} className="mr-1" />
                          Attachments
                        </Button>
                      </div>

                      {showFileUploadForId === appointment.id && (
                        <div className="mt-4 pt-4 border-t border-gray-200">
                          <FileUpload
                            onFileUploaded={(file) => {
                              if (file) {
                                setAppointmentDocuments((prev) => ({
                                  ...prev,
                                  [appointment.id]: [...(prev[appointment.id] || []), file]
                                }))
                              }
                            }}
                            documentType="BookingFile"
                            linkedEntityType="Order"
                            linkedEntityId={appointment.id}
                            disabled={false}
                          />

                          {appointmentDocuments[appointment.id] && appointmentDocuments[appointment.id].length > 0 && (
                            <div className="mt-4">
                              <p className="text-sm font-medium text-text-primary mb-2">Attached Files:</p>
                              <div className="space-y-2">
                                {appointmentDocuments[appointment.id].map((doc) => (
                                  <div key={doc.id} className="flex items-center gap-3 p-2 bg-gray-50 rounded-lg">
                                    <span className="text-lg">{documentService.getFileIcon(doc.contentType)}</span>
                                    <div className="flex-1 min-w-0">
                                      <p className="text-sm text-text-primary truncate">{doc.originalFileName}</p>
                                      <p className="text-xs text-text-secondary">{documentService.formatFileSize(doc.fileSize)}</p>
                                    </div>
                                    <Button
                                      size="xs"
                                      variant="ghost"
                                      onClick={() => documentService.downloadAndSave(doc.id, doc.originalFileName, token)}
                                    >
                                      Download
                                    </Button>
                                  </div>
                                ))}
                              </div>
                            </div>
                          )}

                          {loadingDocuments[appointment.id] && (
                            <div className="mt-2 text-sm text-text-secondary">Loading attachments...</div>
                          )}
                        </div>
                      )}
                    </>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </MainContent>
  )
}
