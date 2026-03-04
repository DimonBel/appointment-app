import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Avatar } from '../../components/ui/Avatar'
import { Loader } from '../../components/ui/Loader'
import { appointmentService } from '../../services/appointmentService'
import { Users, ChevronLeft, ChevronRight, Calendar, Stethoscope, AlertCircle, Grid3x3, Clock, X } from 'lucide-react'

// Time slots from 08:00 to 17:00 (working hours) - hourly intervals only
const TIME_SLOTS = [
  '08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00'
]

const DAYS_OF_WEEK = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']

const statusConfig = {
  0: { text: 'Pending', color: 'bg-yellow-100 text-yellow-800', borderColor: 'border-l-yellow-500' },
  1: { text: 'Approved', color: 'bg-green-100 text-green-800', borderColor: 'border-l-green-500' },
  2: { text: 'Declined', color: 'bg-red-100 text-red-800', borderColor: 'border-l-red-500' },
  3: { text: 'Cancelled', color: 'bg-red-600 text-white', borderColor: 'border-l-red-600' },
  4: { text: 'Completed', color: 'bg-blue-100 text-blue-800', borderColor: 'border-l-blue-500' },
  5: { text: 'No-show', color: 'bg-orange-100 text-orange-800', borderColor: 'border-l-orange-500' },
}

export const ClientPanel = () => {
  const navigate = useNavigate()
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)

  // Schedule Matrix State
  const [activeTab, setActiveTab] = useState('schedule')
  const [selectedWeekStart, setSelectedWeekStart] = useState(getMondayOfWeek(new Date()))
  const [orders, setOrders] = useState([])
  const [scheduleLoading, setScheduleLoading] = useState(false)
  const [selectedBooking, setSelectedBooking] = useState(null)
  const [actionLoadingId, setActionLoadingId] = useState(null)

  function getMondayOfWeek(date) {
    const d = new Date(date)
    const day = d.getDay()
    const diff = d.getDate() - day + (day === 0 ? -6 : 1) // Adjust when day is Sunday
    d.setDate(diff)
    d.setHours(0, 0, 0, 0)
    return d
  }

  function addDays(date, days) {
    const result = new Date(date)
    result.setDate(result.getDate() + days)
    return result
  }

  function getDateKey(date) {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) return null
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
  }

  function getTimeSlot(date) {
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) return null
    const hours = String(date.getHours()).padStart(2, '0')
    const minutes = String(date.getMinutes()).padStart(2, '0')
    return `${hours}:${minutes}`
  }

  useEffect(() => {
    if (activeTab === 'schedule') {
      loadOrders()
    }
  }, [activeTab, selectedWeekStart, token])

  const loadOrders = async () => {
    setScheduleLoading(true)
    try {
      const weekEnd = addDays(selectedWeekStart, 7)

      // Fetch only orders for the current client within the selected week
      const allOrders = await appointmentService.getAllOrdersForManagement(
        token,
        null,
        1,
        500,
        'scheduledDate',
        true,
        selectedWeekStart,
        weekEnd
      )

      // Filter orders for current client
      const filteredOrders = Array.isArray(allOrders) ? allOrders.filter(order => {
        if (!order.scheduledDateTime) return false
        const orderDate = new Date(order.scheduledDateTime)

        // Check if order belongs to this client
        const orderClientId = String(order.clientId || '').toLowerCase()
        const currentUserId = String(currentUser?.id || '').toLowerCase()

        const belongsToClient = orderClientId === currentUserId

        return belongsToClient
      }) : []

      setOrders(filteredOrders)
    } catch (error) {
      console.error('Failed to load orders:', error)
      setOrders([])
    } finally {
      setScheduleLoading(false)
    }
  }

  const handleCancelBooking = async (orderId) => {
    if (!orderId) return
    if (!confirm('Are you sure you want to cancel this booking?')) return
    try {
      setActionLoadingId(orderId)
      await appointmentService.cancelOrder(orderId, token)
      await loadOrders()
      setSelectedBooking(null)
    } catch (error) {
      console.error('Error cancelling booking:', error)
      alert(error?.response?.data?.message || 'Failed to cancel booking')
    } finally {
      setActionLoadingId(null)
    }
  }

  const handleReschedule = async (orderId, newDateTime, notes) => {
    if (!orderId || !newDateTime) return
    try {
      setActionLoadingId(orderId)
      await appointmentService.rescheduleOrder(orderId, newDateTime, notes, token)
      await loadOrders()
      setSelectedBooking(null)
    } catch (error) {
      console.error('Error rescheduling booking:', error)
      alert(error?.response?.data?.message || 'Failed to reschedule booking')
    } finally {
      setActionLoadingId(null)
    }
  }

  // Schedule Matrix Computation
  const getScheduleMatrix = () => {
    const matrix = {}

    // Initialize matrix with days as keys
    DAYS_OF_WEEK.forEach(day => {
      matrix[day] = {}
      TIME_SLOTS.forEach(slot => {
        matrix[day][slot] = null
      })
    })

    // Populate matrix with orders
    orders.forEach(order => {
      if (!order.scheduledDateTime) return

      const orderDate = new Date(order.scheduledDateTime)
      const dayIndex = orderDate.getDay() // 0 = Sunday, 1 = Monday, etc.
      const dayName = DAYS_OF_WEEK[(dayIndex + 6) % 7] // Convert to Monday-based (0 = Monday)

      const timeSlot = getTimeSlot(orderDate)
      if (!timeSlot || !matrix[dayName]) return

      const slotIndex = TIME_SLOTS.indexOf(timeSlot)
      if (slotIndex < 0) return

      const durationSlots = Math.ceil((order.durationMinutes || 60) / 60)

      for (let i = 0; i < durationSlots; i++) {
        if (slotIndex + i < TIME_SLOTS.length) {
          const slotTime = TIME_SLOTS[slotIndex + i]
          matrix[dayName][slotTime] = {
            doctorName: getDoctorName(order),
            doctorAvatar: getDoctorAvatar(order),
            status: order.status,
            isFirstSlot: i === 0,
            isLastSlot: i === durationSlots - 1,
            totalSlots: durationSlots,
            durationMinutes: order.durationMinutes,
            appointment: order,
          }
        }
      }
    })

    return matrix
  }

  const getDoctorName = (order) => {
    const professional = order.professional
    if (professional) {
      return `${professional.firstName || ''} ${professional.lastName || ''}`.trim() || 'Doctor'
    }
    return 'Doctor'
  }

  const getDoctorAvatar = (order) => {
    const professional = order.professional
    if (professional?.avatarUrl) {
      return professional.avatarUrl
    }
    return null
  }

  const scheduleMatrix = getScheduleMatrix()

  const navigateWeek = (direction) => {
    const newDate = addDays(selectedWeekStart, direction * 7)
    setSelectedWeekStart(newDate)
  }

  return (
    <>
      <MainContent>
      <SectionHeader
        title="My Schedule"
        subtitle="View and manage your upcoming appointments"
      />

      {/* Tab Navigation */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          onClick={() => setActiveTab('schedule')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${activeTab === 'schedule'
              ? 'bg-primary-dark text-white shadow-md'
              : 'bg-white border border-gray-300 text-text-secondary hover:bg-gray-50'
            }`}
        >
          <Grid3x3 size={18} />
          Schedule Matrix
        </button>
      </div>

      {activeTab === 'schedule' && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Calendar size={18} className="text-primary-dark" />
                <h3 className="text-lg font-semibold text-text-primary">My Appointments</h3>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => navigateWeek(-1)}
                  className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                >
                  <ChevronLeft size={20} />
                </button>
                <span className="font-medium text-text-primary min-w-[200px] text-center text-sm">
                  {selectedWeekStart.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                  {' - '}
                  {addDays(selectedWeekStart, 6).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                </span>
                <button
                  onClick={() => navigateWeek(1)}
                  className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                >
                  <ChevronRight size={20} />
                </button>
              </div>
            </div>

            {scheduleLoading ? (
              <div className="flex justify-center py-12">
                <Loader size="lg" />
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full min-w-[1000px]">
                  <thead>
                    <tr className="border-b border-gray-200 text-sm text-text-secondary">
                      <th className="text-left py-3 px-3 w-20 font-medium">Time</th>
                      {DAYS_OF_WEEK.map((day, index) => {
                        const dayDate = addDays(selectedWeekStart, index)
                        const isToday = getDateKey(dayDate) === getDateKey(new Date())
                        return (
                          <th key={day} className={`py-3 px-2 text-center font-medium min-w-[130px] ${isToday ? 'bg-blue-50' : ''}`}>
                            <div className="text-xs text-text-secondary">{day.slice(0, 3)}</div>
                            <div className={`text-sm font-semibold ${isToday ? 'text-blue-600' : 'text-text-primary'}`}>
                              {dayDate.getDate()}
                            </div>
                          </th>
                        )
                      })}
                    </tr>
                  </thead>
                  <tbody>
                    {TIME_SLOTS.map((slot) => (
                      <tr key={slot} className="border-b border-gray-100">
                        <td className="py-2 px-3 text-sm font-medium text-text-secondary whitespace-nowrap">
                          {slot}
                        </td>
                        {DAYS_OF_WEEK.map((day, index) => {
                          const cellData = scheduleMatrix[day]?.[slot]
                          const dayDate = addDays(selectedWeekStart, index)
                          const isToday = getDateKey(dayDate) === getDateKey(new Date())

                          if (!cellData) {
                            return (
                              <td key={`${day}-${slot}`} className={`py-1 px-1 min-w-[130px] ${isToday ? 'bg-blue-50/30' : ''}`}>
                                <div className="h-14 bg-gray-50 rounded"></div>
                              </td>
                            )
                          }

                          const { doctorName, doctorAvatar, status, isFirstSlot, appointment } = cellData
                          const statusInfo = statusConfig[status] || statusConfig[0]

                          if (!isFirstSlot) {
                            return (
                              <td key={`${day}-${slot}`} className={`py-1 px-1 min-w-[130px] ${isToday ? 'bg-blue-50/30' : ''}`}>
                                <div className={`h-14 rounded ${statusInfo.color} opacity-50`}></div>
                              </td>
                            )
                          }

                          return (
                            <td key={`${day}-${slot}`} className={`py-1 px-1 min-w-[130px] ${isToday ? 'bg-blue-50/30' : ''}`}>
                              <div
                                className={`h-14 rounded p-2 border-l-4 ${statusInfo.borderColor} ${statusInfo.color} cursor-pointer hover:opacity-90 transition-opacity`}
                                onClick={() => appointment && setSelectedBooking(appointment)}
                              >
                                <div className="flex items-center gap-2">
                                  <Avatar src={doctorAvatar} alt={doctorName} size={24} />
                                  <div className="flex-1 min-w-0">
                                    <div className="text-xs font-semibold text-text-primary truncate">
                                      {doctorName}
                                    </div>
                                    <div className="text-xs text-text-secondary mt-0.5">
                                      {statusInfo.text}
                                    </div>
                                  </div>
                                </div>
                              </div>
                            </td>
                          )
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {orders.length === 0 && (
              <div className="text-center py-12">
                <Calendar size={48} className="mx-auto text-gray-300 mb-3" />
                <h3 className="text-lg font-medium text-text-primary mb-2">No Appointments</h3>
                <p className="text-text-secondary">
                  You don't have any appointments scheduled for this week
                </p>
              </div>
            )}

            <div className="mt-4 flex flex-wrap gap-4 text-xs text-text-secondary">
              <div className="flex items-center gap-1">
                <span className="w-4 h-4 bg-yellow-100 text-yellow-800 rounded flex items-center justify-center">•</span>
                <span>Pending</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="w-4 h-4 bg-green-100 text-green-800 rounded flex items-center justify-center">•</span>
                <span>Approved</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="w-4 h-4 bg-red-100 text-red-800 rounded flex items-center justify-center">•</span>
                <span>Declined</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="w-4 h-4 bg-blue-100 text-blue-800 rounded flex items-center justify-center">•</span>
                <span>Completed</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="w-4 h-4 bg-red-600 text-white rounded flex items-center justify-center">•</span>
                <span>Cancelled</span>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
      </MainContent>

      {/* Booking Detail Modal - rendered outside MainContent to avoid overflow issues */}
      {selectedBooking && (
        <div className="fixed inset-0 bg-black/30 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl max-w-md w-full p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-text-primary">Appointment Details</h3>
              <button
                onClick={() => setSelectedBooking(null)}
                className="p-1 hover:bg-gray-100 rounded-lg transition-colors"
              >
                <X size={20} className="text-text-secondary" />
              </button>
            </div>

            <div className="space-y-4">
              <div className="flex items-center gap-3">
                <Avatar
                  src={getDoctorAvatar(selectedBooking)}
                  alt={getDoctorName(selectedBooking)}
                  size={48}
                />
                <div>
                  <p className="font-medium text-text-primary">{getDoctorName(selectedBooking)}</p>
                  <p className="text-sm text-text-secondary">Doctor</p>
                </div>
              </div>

              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm">
                  <Calendar size={16} className="text-text-secondary" />
                  <span className="text-text-primary">
                    {new Date(selectedBooking.scheduledDateTime).toLocaleDateString('en-US', {
                      weekday: 'long',
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </span>
                </div>
                <div className="flex items-center gap-2 text-sm">
                  <Clock size={16} className="text-text-secondary" />
                  <span className="text-text-primary">
                    {new Date(selectedBooking.scheduledDateTime).toLocaleTimeString('en-US', {
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </span>
                </div>
              </div>

              {selectedBooking.title && (
                <div>
                  <p className="text-sm font-medium text-text-secondary mb-1">Service</p>
                  <p className="text-sm text-text-primary">{selectedBooking.title}</p>
                </div>
              )}

              {selectedBooking.description && (
                <div>
                  <p className="text-sm font-medium text-text-secondary mb-1">Description</p>
                  <p className="text-sm text-text-primary">{selectedBooking.description}</p>
                </div>
              )}

              <div>
                <p className="text-sm font-medium text-text-secondary mb-1">Status</p>
                <span className={`inline-block px-3 py-1 rounded-full text-xs font-medium ${statusConfig[selectedBooking.status]?.color || 'bg-gray-100 text-gray-700'
                  }`}>
                  {statusConfig[selectedBooking.status]?.text || 'Unknown'}
                </span>
              </div>

              {(selectedBooking.status === 0 || selectedBooking.status === 1) && (
                <div className="flex gap-2 pt-4">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => handleCancelBooking(selectedBooking.id)}
                    disabled={actionLoadingId === selectedBooking.id}
                  >
                    {actionLoadingId === selectedBooking.id ? 'Cancelling...' : 'Cancel'}
                  </Button>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </>
  )
}