import React, { useEffect, useMemo, useState } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent } from '../../components/ui/Card'
import { Loader } from '../../components/ui/Loader'
import { Avatar } from '../../components/ui/Avatar'
import { appointmentService } from '../../services/appointmentService'
import { userService } from '../../services/userService'
import { DocumentManagement } from './DocumentManagement'
import { Users, CalendarCheck, ShieldOff, Clock, UserRound, Grid3x3, ChevronLeft, ChevronRight, ArrowUpDown, ArrowUp, ArrowDown, Filter, X, FileText } from 'lucide-react'

const statusConfig = {
  0: { text: 'Pending', color: 'bg-yellow-100 text-yellow-800' },
  1: { text: 'Approved', color: 'bg-green-100 text-green-800' },
  2: { text: 'Declined', color: 'bg-red-100 text-red-800' },
  3: { text: 'Cancelled', color: 'bg-red-600 text-white' },
  4: { text: 'Completed', color: 'bg-blue-100 text-blue-800' },
  5: { text: 'No-show', color: 'bg-orange-100 text-orange-800' },
}

const STATUS_OPTIONS = [
  { value: null, label: 'All Statuses' },
  { value: 0, label: 'Pending' },
  { value: 1, label: 'Approved' },
  { value: 2, label: 'Declined' },
  { value: 3, label: 'Cancelled' },
  { value: 4, label: 'Completed' },
  { value: 5, label: 'No-show' },
]

// Time slots from 08:00 to 17:00 (working hours)
const TIME_SLOTS = [
  '08:00', '08:30', '09:00', '09:30', '10:00', '10:30', '11:00', '11:30',
  '12:00', '12:30', '13:00', '13:30', '14:00', '14:30', '15:00', '15:30',
  '16:00', '16:30', '17:00'
]

const ITEMS_PER_PAGE = 10

const getDateKey = (date) => {
  if (!(date instanceof Date) || Number.isNaN(date.getTime())) return null
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

const getTimeSlot = (date) => {
  if (!(date instanceof Date) || Number.isNaN(date.getTime())) return null
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${hours}:${minutes}`
}

const normalizeId = (value) => (value ? String(value).toLowerCase() : null)

export const ManagementPanel = () => {
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)
  const isAdmin = currentUser?.roles?.includes('Admin')
  const isManagement = currentUser?.roles?.includes('Management') || isAdmin

  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [activeTab, setActiveTab] = useState('schedule')
  const [clientOrders, setClientOrders] = useState([])
  const [doctorSchedules, setDoctorSchedules] = useState([])
  const [selectedDate, setSelectedDate] = useState(new Date())
  const [selectedAppointment, setSelectedAppointment] = useState(null)
  const [showAppointmentModal, setShowAppointmentModal] = useState(false)

  // Pagination and filtering
  const [currentPage, setCurrentPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState(null)

  useEffect(() => {
    if (!isManagement || !token) return
    loadManagementData()
  }, [isManagement, token, selectedDate, currentPage, statusFilter])

  const getDisplayName = (firstName, lastName, userName, fallback = 'User') => {
    // Always prioritize userName/nickname as the primary display name
    // This ensures the exact nickname from Identity service is displayed
    if (userName && !userName.startsWith('user_') && !userName.startsWith('client_')) {
      return userName
    }

    const fullName = `${firstName || ''} ${lastName || ''}`.trim()

    // Skip generic names and use fallback
    const genericNames = ['doctor profile', 'user profile', 'client', 'doctor', 'user', 'professional', 'client user']
    if (genericNames.includes(fullName.toLowerCase())) {
      return fallback
    }

    return fullName || fallback
  }

  const loadManagementData = async () => {
    setLoading(true)
    setLoadError('')

    try {
      const [allOrders, professionals, allAvailabilities, allUsers] = await Promise.all([
        appointmentService.getAllOrdersForManagement(token, statusFilter, 1, 500, 'scheduledDate', true),
        appointmentService.getProfessionals(token),
        appointmentService.getAllAvailabilities(token),
        userService.getAllUsers(token),
      ])

      const professionalList = Array.isArray(professionals) ? professionals : []
      const orders = Array.isArray(allOrders) ? allOrders : []
      const availabilities = Array.isArray(allAvailabilities) ? allAvailabilities : []
      const users = Array.isArray(allUsers) ? allUsers : []

      const usersById = Object.fromEntries(users.map((user) => [user.id, user]))

      // Build schedules for doctors
      const groupedAvailabilities = {}
      availabilities.forEach((av) => {
        if (!groupedAvailabilities[av.professionalId]) {
          groupedAvailabilities[av.professionalId] = []
        }
        groupedAvailabilities[av.professionalId].push(av)
      })

      const schedulesWithProfessionals = professionalList.map((professional) => {
        const availList = groupedAvailabilities[professional.id] || []
        // Prioritize user data from Identity service (usersById) over local database (professional.user)
        // This ensures we display the actual nicknames from Identity service, not generic usernames from local DB
        const doctorUser = usersById[professional.userId] || professional.user

        const doctorName = getDisplayName(
          doctorUser?.firstName,
          doctorUser?.lastName,
          doctorUser?.userName,
          'Doctor'
        )

        return {
          id: professional.id,
          userId: professional.userId,
          doctorName,
          doctorAvatar: doctorUser?.avatarUrl || null,
          title: professional.title || 'Doctor',
          specialization: professional.specialization || 'General',
          hourlyRate: professional.hourlyRate || 0,
          experienceYears: professional.experienceYears || 0,
          schedules: availList.map((item) => ({
            day: item.dayOfWeek,
            slot: `${String(item.startTime).slice(0, 5)}-${String(item.endTime).slice(0, 5)}`,
          })),
        }
      })

      // Filter out generic/shadow doctor entries (created when user doesn't exist in local DB)
      const filteredSchedules = schedulesWithProfessionals.filter((doctor) => {
        // Skip doctors with generic names from shadow users
        const isGenericName = doctor.doctorName === 'Doctor' ||
                            doctor.doctorName === 'Doctor Profile' ||
                            doctor.doctorName === 'User' ||
                            doctor.doctorName.startsWith('user_') ||
                            doctor.doctorName.startsWith('client_')
        // Also skip if the user data indicates it's a shadow user
        const doctorUser = usersById[doctor.userId]
        const isShadowUser = doctorUser?.firstName === 'Doctor' && doctorUser?.lastName === 'Profile'

        return !isGenericName && !isShadowUser
      })

      setDoctorSchedules(filteredSchedules)

      // Build enriched orders - use data from order itself first
      const enrichedOrders = orders.map((order) => {
        // Always prefer data from usersById (Identity service) over order data
        const clientData = usersById[order.clientId] || order.client
        const doctorData = usersById[order.professionalId] || order.professional

        const clientName = getDisplayName(
          clientData?.firstName,
          clientData?.lastName,
          clientData?.userName,
          'Client'
        )

        const doctorName = getDisplayName(
          doctorData?.firstName,
          doctorData?.lastName,
          doctorData?.userName,
          'Doctor'
        )

        const scheduled = order.scheduledDateTime ? new Date(order.scheduledDateTime) : null
        const status = order.status ?? 0

        return {
          id: order.id,
          professionalId: order.professionalId,
          clientName,
          clientAvatar: clientData?.avatarUrl || clientData?.profilePictureUrl || null,
          serviceType: order.title || 'General consultation',
          timing: scheduled
            ? `${scheduled.toLocaleDateString()} ${scheduled.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
            : '-',
          status,
          statusConfig: statusConfig[status] || statusConfig[0],
          doctorName,
          doctorAvatar: doctorData?.avatarUrl || doctorData?.profilePictureUrl || null,
          scheduledAt: scheduled ? scheduled.getTime() : 0,
          scheduledDate: getDateKey(scheduled),
          scheduledTime: getTimeSlot(scheduled),
          durationMinutes: order.durationMinutes || 30,
        }
      }).filter((order) => {
        // Filter out orders with generic/shadow doctors
        const isGenericDoctor = order.doctorName === 'Doctor' ||
                               order.doctorName === 'Doctor Profile' ||
                               order.doctorName === 'User' ||
                               order.doctorName.startsWith('user_') ||
                               order.doctorName.startsWith('client_')

        const doctorData = usersById[order.professionalId]
        const isShadowUser = doctorData?.firstName === 'Doctor' && doctorData?.lastName === 'Profile'

        return !isGenericDoctor && !isShadowUser
      }).sort((left, right) => (left.scheduledAt || 0) - (right.scheduledAt || 0)) // Ascending: oldest first, newest last

      setClientOrders(enrichedOrders)
    } catch (error) {
      console.error('Failed to load management data:', error)
      setLoadError(error?.response?.data?.message || 'Failed to load management data')
      setClientOrders([])
      setDoctorSchedules([])
    } finally {
      setLoading(false)
    }
  }

  const dayLabel = useMemo(
    () => ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
    []
  )

  const navigateDate = (days) => {
    const newDate = new Date(selectedDate)
    newDate.setDate(newDate.getDate() + days)
    setSelectedDate(newDate)
  }

  // Filter orders: only show Pending, Approved, Completed (not Cancelled, Declined, No-show)
  const activeStatuses = [0, 1, 4] // Pending, Approved, Completed
  const filteredOrders = clientOrders.filter((order) => activeStatuses.includes(order.status))

  const getScheduleMatrix = () => {
    const selectedDateKey = getDateKey(selectedDate)
    const todayOrders = filteredOrders.filter((order) => order.scheduledDate === selectedDateKey)
    const matrix = {}

    doctorSchedules.forEach((doctor) => {
      matrix[doctor.id] = {}
      TIME_SLOTS.forEach((slot) => {
        matrix[doctor.id][slot] = null
      })
    })

    // Create a mapping of userId to professionalId for matching
    // Note: order.professionalId contains the userId (from Identity service), not the Professional entity ID
    const userIdToProfessionalId = {}
    doctorSchedules.forEach((doctor) => {
      if (doctor.userId) {
        // Store both as string and normalized lowercase for matching
        userIdToProfessionalId[String(doctor.userId)] = doctor.id
        const normalizedUserId = normalizeId(doctor.userId)
        if (normalizedUserId) {
          userIdToProfessionalId[normalizedUserId] = doctor.id
        }
      }
    })

    todayOrders.forEach((order) => {
      // order.professionalId is a userId (from Identity service), match against userIdToProfessionalId
      if (!order.professionalId) {
        return
      }

      const orderUserId = String(order.professionalId)
      const normalizedOrderUserId = normalizeId(order.professionalId)

      // Try to match using both the original and normalized userId
      const professionalId = userIdToProfessionalId[orderUserId] || userIdToProfessionalId[normalizedOrderUserId]

      if (professionalId && order.scheduledTime) {
        const startSlot = order.scheduledTime
        const slotIndex = TIME_SLOTS.indexOf(startSlot)
        if (slotIndex < 0) {
          return
        }

        const durationSlots = Math.ceil(order.durationMinutes / 30)

        for (let i = 0; i < durationSlots; i++) {
          if (slotIndex + i < TIME_SLOTS.length) {
            const slotTime = TIME_SLOTS[slotIndex + i]
            if (matrix[professionalId]) {
              matrix[professionalId][slotTime] = {
                clientName: order.clientName,
                status: order.status,
                isFirstSlot: i === 0,
                isLastSlot: i === durationSlots - 1,
                totalSlots: durationSlots,
                durationMinutes: order.durationMinutes,
                appointment: order, // Include full appointment details
              }
            }
          }
        }
      }
    })

    return matrix
  }

  const scheduleMatrix = getScheduleMatrix()

  // Pagination for client orders
  const totalPages = Math.ceil(clientOrders.length / ITEMS_PER_PAGE)
  const paginatedOrders = clientOrders.slice(
    (currentPage - 1) * ITEMS_PER_PAGE,
    currentPage * ITEMS_PER_PAGE
  )

  if (!isManagement) {
    return (
      <MainContent>
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <ShieldOff size={64} className="mx-auto text-red-500 mb-4" />
            <h2 className="text-2xl font-semibold text-text-primary mb-2">Access Denied</h2>
            <p className="text-text-secondary">You don't have permission to access management panel.</p>
          </div>
        </div>
      </MainContent>
    )
  }

  return (
    <MainContent>
      <SectionHeader
        title="Management Panel"
        subtitle="Overview of all client appointments and doctor slot availability"
      />

      {loadError && (
        <div className="mb-6 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {loadError}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-12">
          <Loader size="lg" />
        </div>
      ) : (
        <div className="space-y-6">
          <div className="flex flex-wrap gap-2 mb-4">
            <button
              onClick={() => setActiveTab('schedule')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
                activeTab === 'schedule'
                  ? 'bg-primary-dark text-white shadow-md'
                  : 'bg-white border border-gray-300 text-text-secondary hover:bg-gray-50'
              }`}
            >
              <Grid3x3 size={18} />
              Schedule Matrix
            </button>
            <button
              onClick={() => setActiveTab('clients')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
                activeTab === 'clients'
                  ? 'bg-primary-dark text-white shadow-md'
                  : 'bg-white border border-gray-300 text-text-secondary hover:bg-gray-50'
              }`}
            >
              <Users size={18} />
              Clients Appointments
            </button>
            <button
              onClick={() => setActiveTab('doctors')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
                activeTab === 'doctors'
                  ? 'bg-primary-dark text-white shadow-md'
                  : 'bg-white border border-gray-300 text-text-secondary hover:bg-gray-50'
              }`}
            >
              <UserRound size={18} />
              Doctors Availability
            </button>
            <button
              onClick={() => setActiveTab('documents')}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
                activeTab === 'documents'
                  ? 'bg-primary-dark text-white shadow-md'
                  : 'bg-white border border-gray-300 text-text-secondary hover:bg-gray-50'
              }`}
            >
              <FileText size={18} />
              Documents
            </button>
          </div>

          {activeTab === 'schedule' && (
            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center gap-2">
                    <Grid3x3 size={18} className="text-primary-dark" />
                    <h3 className="text-lg font-semibold text-text-primary">Schedule Matrix</h3>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => navigateDate(-1)}
                      className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                    >
                      <ChevronLeft size={20} />
                    </button>
                    <span className="font-medium text-text-primary min-w-[140px] text-center">
                      {selectedDate.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })}
                    </span>
                    <button
                      onClick={() => navigateDate(1)}
                      className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
                    >
                      <ChevronRight size={20} />
                    </button>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full min-w-[1200px]">
                    <thead>
                      <tr className="border-b border-gray-200 text-sm text-text-secondary">
                        <th className="text-left py-2 pr-2 w-20 font-medium">Time</th>
                        {doctorSchedules.map((doctor) => (
                          <th key={doctor.id} className="py-2 px-2 text-center font-medium min-w-[180px]">
                            <div className="flex flex-col items-center gap-1">
                              <Avatar src={doctor.doctorAvatar} alt={doctor.doctorName} size={32} />
                              <span className="text-xs font-medium">{doctor.doctorName}</span>
                            </div>
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {TIME_SLOTS.map((slot) => (
                        <tr key={slot} className="border-b border-gray-100 text-sm">
                          <td className="py-2 pr-2 text-text-secondary font-medium whitespace-nowrap">{slot}</td>
                          {doctorSchedules.map((doctor) => {
                            const cellData = scheduleMatrix[doctor.id]?.[slot]

                            // Skip rendering if this slot is part of a multi-slot appointment (not the first slot)
                            if (cellData && !cellData.isFirstSlot) {
                              return null
                            }

                            // Empty slot
                            if (!cellData) {
                              return (
                                <td key={`${doctor.id}-${slot}`} className="py-2 px-2 border-l border-gray-100" />
                              )
                            }

                            // Occupied slot - calculate rowSpan and styling
                            const statusColor = statusConfig[cellData.status]?.color || 'bg-gray-100 text-gray-700'
                            const rowSpan = cellData.totalSlots || 1
                            const borderRadius = 'rounded-md'

                            return (
                              <td
                                key={`${doctor.id}-${slot}`}
                                rowSpan={rowSpan}
                                className={`py-1 px-2 border-l border-gray-100 align-top`}
                              >
                                <div
                                  className={`p-2 text-xs font-medium ${statusColor} ${borderRadius} flex items-center justify-center cursor-pointer hover:opacity-80 transition-opacity`}
                                  style={{ minHeight: `${rowSpan * 40 - 16}px` }}
                                  onClick={() => {
                                    setSelectedAppointment(cellData.appointment)
                                    setShowAppointmentModal(true)
                                  }}
                                >
                                  <div className="text-center">
                                    <div className="font-semibold underline decoration-dotted">{cellData.clientName}</div>
                                    <div className="text-[10px] mt-1 opacity-75">
                                      {cellData.durationMinutes} min
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

          {activeTab === 'clients' && (
            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center gap-2">
                    <Users size={18} className="text-primary-dark" />
                    <h3 className="text-lg font-semibold text-text-primary">Clients Appointments</h3>
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="flex items-center gap-2">
                      <Filter size={16} className="text-text-secondary" />
                      <select
                        value={statusFilter ?? ''}
                        onChange={(e) => {
                          setStatusFilter(e.target.value === '' ? null : parseInt(e.target.value))
                          setCurrentPage(1)
                        }}
                        className="px-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                      >
                        {STATUS_OPTIONS.map((option) => (
                          <option key={option.value ?? 'all'} value={option.value ?? ''}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                      {statusFilter !== null && (
                        <button
                          onClick={() => {
                            setStatusFilter(null)
                            setCurrentPage(1)
                          }}
                          className="p-1 hover:bg-gray-100 rounded"
                        >
                          <X size={14} />
                        </button>
                      )}
                    </div>
                    <span className="text-sm text-text-secondary">
                      {clientOrders.length} appointments
                    </span>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full min-w-[900px]">
                    <thead>
                      <tr className="border-b border-gray-200 text-sm text-text-secondary">
                        <th className="text-left py-2 pr-2">Client</th>
                        <th className="text-left py-2 pr-2">Service Type</th>
                        <th className="text-left py-2 pr-2">Doctor</th>
                        <th className="text-left py-2 pr-2">Timing</th>
                        <th className="text-left py-2 pr-2">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {paginatedOrders.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="py-6 text-center text-text-secondary">
                            {loading ? 'Loading...' : 'No appointments found'}
                          </td>
                        </tr>
                      ) : (
                        paginatedOrders.map((order) => (
                          <tr key={order.id} className="border-b border-gray-100 text-sm">
                            <td className="py-3 pr-2">
                              <div className="flex items-center gap-2">
                                <Avatar src={order.clientAvatar} alt={order.clientName} size={34} />
                                <span className="text-text-primary font-medium">{order.clientName}</span>
                              </div>
                            </td>
                            <td className="py-3 pr-2 text-text-primary">{order.serviceType}</td>
                            <td className="py-3 pr-2">
                              <div className="flex items-center gap-2">
                                <Avatar src={order.doctorAvatar} alt={order.doctorName} size={34} />
                                <span className="text-text-secondary">{order.doctorName}</span>
                              </div>
                            </td>
                            <td className="py-3 pr-2 text-text-secondary">{order.timing}</td>
                            <td className="py-3 pr-2">
                              <span className={`px-2 py-1 rounded-full text-xs font-medium ${order.statusConfig.color}`}>
                                {order.statusConfig.text}
                              </span>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>

                {totalPages > 1 && (
                  <div className="flex items-center justify-between mt-4">
                    <div className="text-sm text-text-secondary">
                      Showing {(currentPage - 1) * ITEMS_PER_PAGE + 1} to {Math.min(currentPage * ITEMS_PER_PAGE, clientOrders.length)} of {clientOrders.length}
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setCurrentPage((prev) => Math.max(1, prev - 1))}
                        disabled={currentPage === 1}
                        className="px-3 py-1 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                      >
                        Previous
                      </button>
                      <span className="text-sm text-text-secondary">
                        Page {currentPage} of {totalPages}
                      </span>
                      <button
                        onClick={() => setCurrentPage((prev) => Math.min(totalPages, prev + 1))}
                        disabled={currentPage === totalPages}
                        className="px-3 py-1 border border-gray-300 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                      >
                        Next
                      </button>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {activeTab === 'doctors' && (
            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center gap-2">
                    <CalendarCheck size={18} className="text-primary-dark" />
                    <h3 className="text-lg font-semibold text-text-primary">Doctors Availability Slots</h3>
                  </div>
                  <div className="flex items-center gap-4">
                    <button
                      onClick={() => loadManagementData()}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm bg-primary-dark text-white rounded-lg hover:bg-primary-dark/90 transition-colors"
                    >
                      <Clock size={14} />
                      Refresh
                    </button>
                    <span className="text-sm text-text-secondary">{doctorSchedules.length} doctors</span>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <table className="w-full min-w-[1100px]">
                    <thead>
                      <tr className="border-b border-gray-200 text-sm text-text-secondary">
                        <th className="text-left py-2 pr-2">Doctor</th>
                        <th className="text-left py-2 pr-2">Specialization</th>
                        <th className="text-left py-2 pr-2">Consultation Fee</th>
                        <th className="text-left py-2 pr-2">Experience</th>
                        <th className="text-left py-2 pr-2">Available Slots</th>
                      </tr>
                    </thead>
                    <tbody>
                      {doctorSchedules.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="py-6 text-center text-text-secondary">No doctors found</td>
                        </tr>
                      ) : (
                        doctorSchedules.map((doctor) => (
                          <tr key={doctor.id} className="border-b border-gray-100 text-sm align-top">
                            <td className="py-3 pr-2">
                              <div className="flex items-center gap-2">
                                <Avatar src={doctor.doctorAvatar} alt={doctor.doctorName} size={34} />
                                <div>
                                  <span className="text-text-primary font-medium">{doctor.doctorName}</span>
                                  <p className="text-xs text-text-secondary">{doctor.title}</p>
                                </div>
                              </div>
                            </td>
                            <td className="py-3 pr-2 text-text-primary">{doctor.specialization}</td>
                            <td className="py-3 pr-2">
                              <div className="flex items-center gap-1 text-text-secondary">
                                <span className="font-semibold text-green-600">${doctor.hourlyRate}</span>
                                <span className="text-xs">/consultation</span>
                              </div>
                            </td>
                            <td className="py-3 pr-2 text-text-secondary">{doctor.experienceYears} years</td>
                            <td className="py-3 pr-2">
                              <div className="flex flex-wrap gap-2">
                                {doctor.schedules.length === 0 ? (
                                  <span className="text-text-secondary">No slots configured</span>
                                ) : (
                                  doctor.schedules.map((slot, idx) => (
                                    <span key={`${doctor.id}-${idx}`} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-700">
                                      <Clock size={12} />
                                      {dayLabel[slot.day] || 'Day'} {slot.slot}
                                    </span>
                                  ))
                                )}
                              </div>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'documents' && <DocumentManagement />}
        </div>
      )}

      {/* Appointment Details Modal */}
      {showAppointmentModal && selectedAppointment && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
            <div className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-text-primary">Appointment Details</h3>
                <button
                  onClick={() => {
                    setShowAppointmentModal(false)
                    setSelectedAppointment(null)
                  }}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <X size={20} />
                </button>
              </div>

              <div className="space-y-4">
                <div>
                  <p className="text-sm text-text-secondary">Client</p>
                  <p className="font-medium text-text-primary flex items-center gap-2">
                    <Avatar src={selectedAppointment.clientAvatar} alt={selectedAppointment.clientName} size={32} />
                    {selectedAppointment.clientName}
                  </p>
                </div>

                <div>
                  <p className="text-sm text-text-secondary">Doctor</p>
                  <p className="font-medium text-text-primary flex items-center gap-2">
                    <Avatar src={selectedAppointment.doctorAvatar} alt={selectedAppointment.doctorName} size={32} />
                    {selectedAppointment.doctorName}
                  </p>
                </div>

                <div>
                  <p className="text-sm text-text-secondary">Service</p>
                  <p className="font-medium text-text-primary">{selectedAppointment.serviceType}</p>
                </div>

                <div>
                  <p className="text-sm text-text-secondary">Date & Time</p>
                  <p className="font-medium text-text-primary">{selectedAppointment.timing}</p>
                </div>

                <div>
                  <p className="text-sm text-text-secondary">Duration</p>
                  <p className="font-medium text-text-primary">{selectedAppointment.durationMinutes} minutes</p>
                </div>

                <div>
                  <p className="text-sm text-text-secondary">Status</p>
                  <span className={`inline-block px-2 py-1 rounded-full text-xs font-medium ${selectedAppointment.statusConfig.color}`}>
                    {selectedAppointment.statusConfig.text}
                  </span>
                </div>
              </div>

              <div className="mt-6 flex justify-end">
                <button
                  onClick={() => {
                    setShowAppointmentModal(false)
                    setSelectedAppointment(null)
                  }}
                  className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
                >
                  Close
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </MainContent>
  )
}