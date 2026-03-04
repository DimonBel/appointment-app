import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { useNavigate } from 'react-router-dom'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Avatar } from '../../components/ui/Avatar'
import { Loader } from '../../components/ui/Loader'
import { appointmentService } from '../../services/appointmentService'
import documentService from '../../services/documentService'
import { Users, Search, ChevronLeft, ChevronRight, Eye, Stethoscope, AlertCircle, SortAsc, SortDesc, Grid3x3, Calendar, ChevronUp, ChevronDown, Phone, MapPin, Clock, X } from 'lucide-react'

const ITEMS_PER_PAGE = 10

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

export const DoctorPanel = () => {
  const navigate = useNavigate()
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)
  const isDoctor = currentUser?.roles?.includes('Doctor') || currentUser?.roles?.includes('Professional')

  const [loading, setLoading] = useState(true)
  const [clients, setClients] = useState([])
  const [clientAppointments, setClientAppointments] = useState({}) // Store appointment counts per client
  const [currentPage, setCurrentPage] = useState(1)
  const [searchQuery, setSearchQuery] = useState('')
  const [loadError, setLoadError] = useState('')

  // Sorting
  const [sortField, setSortField] = useState('name')
  const [sortOrder, setSortOrder] = useState('asc')

  // Schedule Matrix State
  const [activeTab, setActiveTab] = useState('clients')
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

  function getProfessionalId() {
    // Check if user has professional entity
    const professionalEntity = currentUser?.professionalEntity
    if (professionalEntity?.id) {
      return professionalEntity.id
    }
    // Fall back to userId if professional entity not set
    return currentUser?.id
  }

  useEffect(() => {
    if (!isDoctor) return
    loadClients()
  }, [isDoctor, token])

  useEffect(() => {
    if (activeTab === 'schedule' && isDoctor) {
      loadOrders()
    }
  }, [activeTab, selectedWeekStart, isDoctor, token])

  const loadOrders = async () => {
    setScheduleLoading(true)
    try {
      const professionalId = getProfessionalId()
      const allOrders = await appointmentService.getAllOrdersForManagement(token, null, 1, 1000, 'scheduledDate', true)
      
      // Filter orders for current doctor and within selected week
      const weekEnd = addDays(selectedWeekStart, 7)
      const filteredOrders = Array.isArray(allOrders) ? allOrders.filter(order => {
        if (!order.scheduledDateTime) return false
        const orderDate = new Date(order.scheduledDateTime)
        const orderDateKey = getDateKey(orderDate)
        
        // Check if order belongs to this doctor
        const orderUserId = String(order.professionalId || '').toLowerCase()
        const doctorUserId = String(currentUser?.id || '').toLowerCase()
        const doctorProfessionalId = String(professionalId || '').toLowerCase()
        
        const belongsToDoctor = orderUserId === doctorUserId || orderUserId === doctorProfessionalId
        
        // Check if order is within the selected week
        const isInWeek = orderDate >= selectedWeekStart && orderDate < weekEnd
        
        return belongsToDoctor && isInWeek
      }) : []
      
      setOrders(filteredOrders)
    } catch (error) {
      console.error('Failed to load orders:', error)
      setOrders([])
    } finally {
      setScheduleLoading(false)
    }
  }

  const handleApproveBooking = async (orderId) => {
    if (!orderId) return
    const reason = prompt('Enter approval reason (optional):') || ''
    try {
      setActionLoadingId(orderId)
      await appointmentService.approveOrder(orderId, reason, token)
      await loadOrders()
      setSelectedBooking(null)
    } catch (error) {
      console.error('Error approving booking:', error)
      alert(error?.response?.data?.message || 'Failed to approve booking')
    } finally {
      setActionLoadingId(null)
    }
  }

  const handleDeclineBooking = async (orderId) => {
    if (!orderId) return
    const reason = prompt('Enter decline reason:') || 'Declined by doctor'
    if (!reason) return
    try {
      setActionLoadingId(orderId)
      await appointmentService.declineOrder(orderId, reason, token)
      await loadOrders()
      setSelectedBooking(null)
    } catch (error) {
      console.error('Error declining booking:', error)
      alert(error?.response?.data?.message || 'Failed to decline booking')
    } finally {
      setActionLoadingId(null)
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

  const handleCompleteBooking = async (orderId) => {
    if (!orderId) return
    const notes = prompt('Enter completion notes (optional):') || ''
    if (!confirm('Mark this booking as completed?')) return
    try {
      setActionLoadingId(orderId)
      await appointmentService.completeOrder(orderId, notes, token)
      await loadOrders()
      setSelectedBooking(null)
    } catch (error) {
      console.error('Error completing booking:', error)
      alert(error?.response?.data?.message || 'Failed to complete booking')
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

  const loadClients = async () => {
    setLoading(true)
    setLoadError('')
    try {
      const data = await appointmentService.getClientsByProfessional(currentUser.id, token)
      const clientsList = Array.isArray(data) ? data : []
      setClients(clientsList)

      // Load appointment counts for all clients in parallel
      const appointmentPromises = clientsList.map(async (client) => {
        try {
          const orders = await appointmentService.getOrdersByClient(client.id, token, null, 1, 100, currentUser.id)
          return {
            clientId: client.id,
            data: {
              total: Array.isArray(orders) ? orders.length : 0,
              upcoming: Array.isArray(orders) ? orders.filter(o => new Date(o.scheduledDateTime) > new Date()).length : 0
            }
          }
        } catch (err) {
          console.error(`Failed to load appointments for client ${client.id}:`, err)
          return {
            clientId: client.id,
            data: { total: 0, upcoming: 0 }
          }
        }
      })

      const appointmentResults = await Promise.all(appointmentPromises)
      const appointmentCounts = {}
      appointmentResults.forEach(({ clientId, data }) => {
        appointmentCounts[clientId] = data
      })
      setClientAppointments(appointmentCounts)
    } catch (error) {
      console.error('Failed to load clients:', error)
      setClients([])
      setLoadError(error?.response?.data?.message || error?.message || 'Failed to load clients')
    } finally {
      setLoading(false)
    }
  }

  const handleViewClient = (clientId) => {
    navigate(`/doctor-panel/client/${clientId}`)
  }

  const clearSearch = () => {
    setSearchQuery('')
    setCurrentPage(1)
  }

  const toggleSort = (field) => {
    if (sortField === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc')
    } else {
      setSortField(field)
      setSortOrder('asc')
    }
  }

  // Filter and sort clients
  const processedClients = clients.filter((client) => {
    if (!searchQuery) return true
    const query = searchQuery.toLowerCase()
    return (
      client.email?.toLowerCase().includes(query) ||
      client.userName?.toLowerCase().includes(query) ||
      `${client.firstName} ${client.lastName}`.toLowerCase().includes(query)
    )
  }).sort((a, b) => {
    let compareValue = 0

    switch (sortField) {
      case 'name':
        const nameA = `${a.firstName} ${a.lastName}`.toLowerCase()
        const nameB = `${b.firstName} ${b.lastName}`.toLowerCase()
        compareValue = nameA.localeCompare(nameB)
        break
      case 'date':
        compareValue = new Date(a.createdAt) - new Date(b.createdAt)
        break
      case 'appointments':
        compareValue = (clientAppointments[a.id]?.total || 0) - (clientAppointments[b.id]?.total || 0)
        break
      default:
        compareValue = 0
    }

    return sortOrder === 'asc' ? compareValue : -compareValue
  })

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
      
      const durationSlots = Math.ceil((order.durationMinutes || 30) / 30)
      
      for (let i = 0; i < durationSlots; i++) {
                  if (slotIndex + i < TIME_SLOTS.length) {
                    const slotTime = TIME_SLOTS[slotIndex + i]
                    matrix[dayName][slotTime] = {
                      clientName: order.client ? `${order.client.firstName} ${order.client.lastName}`.trim() : 'Unknown',
                      status: order.status,
                      isFirstSlot: i === 0,
                      isLastSlot: i === durationSlots - 1,
                      totalSlots: durationSlots,
                      durationMinutes: order.durationMinutes,
                      appointment: order,
                    }
                  }
                }    })
    
    return matrix
  }

  const scheduleMatrix = getScheduleMatrix()

  const navigateWeek = (direction) => {
    const newDate = addDays(selectedWeekStart, direction * 7)
    setSelectedWeekStart(newDate)
  }

  const totalPages = Math.ceil(processedClients.length / ITEMS_PER_PAGE)
  const paginatedClients = processedClients.slice(
    (currentPage - 1) * ITEMS_PER_PAGE,
    currentPage * ITEMS_PER_PAGE
  )

  if (!isDoctor) {
    return (
      <MainContent>
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <Stethoscope size={64} className="mx-auto text-red-500 mb-4" />
            <h2 className="text-2xl font-semibold text-text-primary mb-2">Access Denied</h2>
            <p className="text-text-secondary">This page is only accessible to doctors and professionals.</p>
          </div>
        </div>
      </MainContent>
    )
  }

  return (
    <MainContent>
      <SectionHeader
        title="Doctor Panel"
        subtitle={`${processedClients.length} client${processedClients.length !== 1 ? 's' : ''}`}
      />

      {/* Tab Navigation */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          onClick={() => setActiveTab('clients')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg font-medium transition-colors ${
            activeTab === 'clients'
              ? 'bg-primary-dark text-white shadow-md'
              : 'bg-white border border-gray-300 text-text-secondary hover:bg-gray-50'
          }`}
        >
          <Users size={18} />
          Clients
        </button>
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
      </div>

      {activeTab === 'clients' && (
        <>
          <SectionHeader
            title="My Clients"
            subtitle={`${processedClients.length} client${processedClients.length !== 1 ? 's' : ''}`}
          />

      {loadError && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl text-red-700 text-sm flex items-center gap-3">
          <AlertCircle size={18} />
          {loadError}
        </div>
      )}

      {/* Clients Table Card */}
      <Card>
        <CardHeader>
          <CardTitle>All Clients</CardTitle>
          <div className="mt-4 flex gap-2">
            <div className="relative flex-1">
              <Search size={18} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder="Search clients..."
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value)
                  setCurrentPage(1)
                }}
                className="w-full pl-10 pr-9 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
              />
              {searchQuery && (
                <button
                  onClick={clearSearch}
                  className="absolute right-2 top-1/2 transform -translate-y-1/2 p-1 hover:bg-gray-100 rounded-md transition-colors"
                >
                  <span className="text-gray-400 text-xs">✕</span>
                </button>
              )}
            </div>
            <div className="flex items-center gap-2 border border-gray-200 rounded-lg px-2">
              <span className="text-xs text-text-secondary ml-1">Sort:</span>
              <button
                onClick={() => toggleSort('name')}
                className={`px-3 py-1.5 rounded text-xs font-medium transition-all ${
                  sortField === 'name'
                    ? 'bg-blue-100 text-blue-700'
                    : 'hover:bg-gray-100 text-text-secondary'
                }`}
              >
                Name
                {sortField === 'name' && (
                  sortOrder === 'asc' ? <SortAsc size={12} className="inline ml-1" /> : <SortDesc size={12} className="inline ml-1" />
                )}
              </button>
              <button
                onClick={() => toggleSort('appointments')}
                className={`px-3 py-1.5 rounded text-xs font-medium transition-all ${
                  sortField === 'appointments'
                    ? 'bg-blue-100 text-blue-700'
                    : 'hover:bg-gray-100 text-text-secondary'
                }`}
              >
                Appointments
                {sortField === 'appointments' && (
                  sortOrder === 'asc' ? <SortAsc size={12} className="inline ml-1" /> : <SortDesc size={12} className="inline ml-1" />
                )}
              </button>
              <button
                onClick={() => toggleSort('date')}
                className={`px-3 py-1.5 rounded text-xs font-medium transition-all ${
                  sortField === 'date'
                    ? 'bg-blue-100 text-blue-700'
                    : 'hover:bg-gray-100 text-text-secondary'
                }`}
              >
                Date
                {sortField === 'date' && (
                  sortOrder === 'asc' ? <SortAsc size={12} className="inline ml-1" /> : <SortDesc size={12} className="inline ml-1" />
                )}
              </button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="text-center py-8">
              <Loader size="lg" />
            </div>
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-gray-200">
                      <th className="text-left py-4 px-5 font-medium text-text-secondary">Client</th>
                      <th className="text-left py-4 px-5 font-medium text-text-secondary">Phone</th>
                      <th className="text-left py-4 px-5 font-medium text-text-secondary">Appointments</th>
                      <th className="text-left py-4 px-5 font-medium text-text-secondary">Upcoming</th>
                      <th className="text-left py-4 px-5 font-medium text-text-secondary">Member Since</th>
                      <th className="text-right py-4 px-5 font-medium text-text-secondary">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {paginatedClients.map((client) => (
                      <tr key={client.id} className="border-b border-gray-100 hover:bg-gray-50">
                        <td className="py-4 px-5">
                          <div className="flex items-center gap-4">
                            <Avatar src={client.avatarUrl} alt={client.userName} size={48} />
                            <div className="min-w-0">
                              <p className="font-medium text-text-primary text-base">
                                {client.firstName} {client.lastName}
                              </p>
                              <p className="text-sm text-text-secondary">@{client.userName}</p>
                              {client.email && (
                                <p className="text-xs text-text-secondary mt-0.5">{client.email}</p>
                              )}
                            </div>
                          </div>
                        </td>
                        <td className="py-4 px-5 text-text-secondary text-sm">
                          {client.phoneNumber || '-'}
                        </td>
                        <td className="py-4 px-5">
                          <span className="bg-blue-100 text-blue-700 px-3 py-1.5 rounded-full text-sm font-medium">
                            {clientAppointments[client.id]?.total || 0}
                          </span>
                        </td>
                        <td className="py-4 px-5">
                          <span className="bg-green-100 text-green-700 px-3 py-1.5 rounded-full text-sm font-medium">
                            {clientAppointments[client.id]?.upcoming || 0}
                          </span>
                        </td>
                        <td className="py-4 px-5 text-sm text-text-secondary">
                          {new Date(client.createdAt).toLocaleDateString()}
                        </td>
                        <td className="py-4 px-5">
                          <div className="flex justify-end gap-2">
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleViewClient(client.id)}
                            >
                              <Eye size={16} />
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between mt-6">
                  <p className="text-sm text-text-secondary">
                    Showing {(currentPage - 1) * ITEMS_PER_PAGE + 1} to{' '}
                    {Math.min(currentPage * ITEMS_PER_PAGE, processedClients.length)} of{' '}
                    {processedClients.length} clients
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                    >
                      <ChevronLeft size={16} />
                    </Button>
                    <span className="px-3 py-2 text-sm text-text-secondary">
                      Page {currentPage} of {totalPages}
                    </span>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                      disabled={currentPage === totalPages}
                    >
                      <ChevronRight size={16} />
                    </Button>
                  </div>
                </div>
              )}

              {paginatedClients.length === 0 && (
                <div className="text-center py-12">
                  <Users size={48} className="mx-auto text-gray-300 mb-3" />
                  <h3 className="text-lg font-medium text-text-primary mb-2">No Clients Found</h3>
                  <p className="text-text-secondary">
                    {searchQuery ? 'Try adjusting your search terms' : 'You don\'t have any clients yet'}
                  </p>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
        </>
      )}

      {activeTab === 'schedule' && (
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Grid3x3 size={18} className="text-primary-dark" />
                <h3 className="text-lg font-semibold text-text-primary">My Schedule</h3>
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
                          
                          const { clientName, status, isFirstSlot, appointment } = cellData
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
                                <div className="text-xs font-semibold text-text-primary truncate">
                                  {clientName || 'Unknown'}
                                </div>
                                <div className="text-xs text-text-secondary mt-0.5">
                                  {statusInfo.text}
                                </div>
                              </div>
                            </td>
                          )
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>

                {orders.length === 0 && (
                  <div className="text-center py-12">
                    <Calendar size={48} className="mx-auto text-gray-300 mb-3" />
                    <h3 className="text-lg font-medium text-text-primary mb-2">No Appointments This Week</h3>
                    <p className="text-text-secondary">You don't have any scheduled appointments for this week.</p>
                  </div>
                )}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Booking Details Modal */}
      {selectedBooking && (
        <div className="fixed inset-0 bg-black/30 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg p-6 w-full max-w-2xl max-h-[90vh] overflow-y-auto shadow-2xl">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-text-primary">Booking Details</h3>
              <button
                onClick={() => setSelectedBooking(null)}
                className="text-text-secondary hover:text-text-primary"
              >
                <X size={24} />
              </button>
            </div>

            <div className="space-y-4">
              {/* Client Information */}
              <div className="flex items-start gap-4 p-4 bg-gray-50 rounded-lg">
                <Avatar 
                  src={selectedBooking.client?.avatarUrl}
                  alt={selectedBooking.client?.firstName || 'Client'}
                  size={64}
                />
                <div className="flex-1">
                  <h4 className="font-semibold text-text-primary text-lg">
                    {selectedBooking.client?.firstName} {selectedBooking.client?.lastName}
                  </h4>
                  <p className="text-text-secondary text-sm">@{selectedBooking.client?.userName || selectedBooking.client?.email}</p>
                  {selectedBooking.client?.email && (
                    <p className="text-text-secondary text-sm mt-1">{selectedBooking.client.email}</p>
                  )}
                  {selectedBooking.client?.phoneNumber && (
                    <p className="text-text-secondary text-sm mt-1 flex items-center gap-2">
                      <Phone size={14} />
                      {selectedBooking.client.phoneNumber}
                    </p>
                  )}
                </div>
              </div>

              {/* Appointment Details */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="p-4 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-2 text-text-secondary text-sm mb-2">
                    <Calendar size={16} />
                    <span>Date</span>
                  </div>
                  <p className="font-medium text-text-primary">
                    {selectedBooking.scheduledDateTime 
                      ? new Date(selectedBooking.scheduledDateTime).toLocaleDateString('en-US', { 
                          weekday: 'long', 
                          year: 'numeric', 
                          month: 'long', 
                          day: 'numeric' 
                        })
                      : '-'}
                  </p>
                </div>

                <div className="p-4 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-2 text-text-secondary text-sm mb-2">
                    <Clock size={16} />
                    <span>Time</span>
                  </div>
                  <p className="font-medium text-text-primary">
                    {selectedBooking.scheduledDateTime 
                      ? new Date(selectedBooking.scheduledDateTime).toLocaleTimeString('en-US', { 
                          hour: '2-digit', 
                          minute: '2-digit' 
                        })
                      : '-'}
                  </p>
                  <p className="text-sm text-text-secondary mt-1">
                    Duration: {selectedBooking.durationMinutes || 30} minutes
                  </p>
                </div>
              </div>

              {/* Status */}
              <div className="p-4 bg-gray-50 rounded-lg">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="flex items-center gap-2 text-text-secondary text-sm mb-2">
                      <Stethoscope size={16} />
                      <span>Status</span>
                    </div>
                    <span className={`px-3 py-1 rounded-full text-sm font-medium ${statusConfig[selectedBooking.status]?.color || statusConfig[0].color}`}>
                      {statusConfig[selectedBooking.status]?.text || 'Unknown'}
                    </span>
                  </div>
                  <div className="text-right">
                    <p className="text-xs text-text-secondary">Booking ID</p>
                    <p className="font-mono text-sm text-text-primary">{selectedBooking.id}</p>
                  </div>
                </div>
              </div>

              {/* Title and Description */}
              {selectedBooking.title && (
                <div className="p-4 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-2 text-text-secondary text-sm mb-2">
                    <Stethoscope size={16} />
                    <span>Appointment Type</span>
                  </div>
                  <p className="font-medium text-text-primary">{selectedBooking.title}</p>
                </div>
              )}

              {selectedBooking.description && (
                <div className="p-4 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-2 text-text-secondary text-sm mb-2">
                    <MapPin size={16} />
                    <span>Location / Description</span>
                  </div>
                  <p className="text-text-primary">{selectedBooking.description}</p>
                </div>
              )}

              {selectedBooking.notes && (
                <div className="p-4 bg-gray-50 rounded-lg">
                  <div className="flex items-center gap-2 text-text-secondary text-sm mb-2">
                    <AlertCircle size={16} />
                    <span>Notes</span>
                  </div>
                  <p className="text-text-primary">{selectedBooking.notes}</p>
                </div>
              )}

              {/* Action Buttons */}
              <div className="flex flex-wrap gap-2 pt-4 border-t border-gray-200">
                {selectedBooking.status === 0 && (
                  <>
                    <Button 
                      variant="primary" 
                      onClick={() => handleApproveBooking(selectedBooking.id)}
                      disabled={actionLoadingId === selectedBooking.id}
                    >
                      {actionLoadingId === selectedBooking.id ? 'Approving...' : 'Approve'}
                    </Button>
                    <Button 
                      variant="danger" 
                      onClick={() => handleDeclineBooking(selectedBooking.id)}
                      disabled={actionLoadingId === selectedBooking.id}
                    >
                      {actionLoadingId === selectedBooking.id ? 'Declining...' : 'Decline'}
                    </Button>
                  </>
                )}
                {selectedBooking.status === 1 && (
                  <>
                    <Button 
                      variant="success" 
                      onClick={() => handleCompleteBooking(selectedBooking.id)}
                      disabled={actionLoadingId === selectedBooking.id}
                    >
                      {actionLoadingId === selectedBooking.id ? 'Completing...' : 'Complete'}
                    </Button>
                    <Button 
                      variant="danger" 
                      onClick={() => handleCancelBooking(selectedBooking.id)}
                      disabled={actionLoadingId === selectedBooking.id}
                    >
                      {actionLoadingId === selectedBooking.id ? 'Cancelling...' : 'Cancel'}
                    </Button>
                  </>
                )}
                {(selectedBooking.status === 0 || selectedBooking.status === 1) && (
                  <Button 
                    variant="outline" 
                    onClick={() => {
                      const dateInput = prompt('Enter new date and time (YYYY-MM-DDTHH:mm), e.g. 2026-02-20T14:30')
                      if (!dateInput) return
                      const parsed = new Date(dateInput)
                      if (Number.isNaN(parsed.getTime())) {
                        alert('Invalid date format')
                        return
                      }
                      const notes = prompt('Reschedule note (optional)') || ''
                      handleReschedule(selectedBooking.id, parsed.toISOString(), notes)
                    }}
                    disabled={actionLoadingId === selectedBooking.id}
                  >
                    Reschedule
                  </Button>
                )}
                <Button 
                  variant="outline" 
                  onClick={() => setSelectedBooking(null)}
                >
                  Close
                </Button>
              </div>

              {/* Creation Date */}
              <div className="text-center text-xs text-text-secondary pt-2">
                Created on {new Date(selectedBooking.createdAt).toLocaleString()}
              </div>
            </div>
          </div>
        </div>
      )}
    </MainContent>
  )
}