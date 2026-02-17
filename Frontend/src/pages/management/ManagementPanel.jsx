import React, { useEffect, useMemo, useState } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent } from '../../components/ui/Card'
import { Loader } from '../../components/ui/Loader'
import { Avatar } from '../../components/ui/Avatar'
import { appointmentService } from '../../services/appointmentService'
import { userService } from '../../services/userService'
import { Users, CalendarCheck, ShieldOff, Clock } from 'lucide-react'

const statusText = {
  0: 'Pending',
  1: 'Approved',
  2: 'Declined',
  3: 'Cancelled',
  4: 'Completed',
  5: 'No-show',
}

export const ManagementPanel = () => {
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)
  const isManagement = currentUser?.roles?.includes('Management')

  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [clientOrders, setClientOrders] = useState([])
  const [doctorSchedules, setDoctorSchedules] = useState([])

  useEffect(() => {
    if (!isManagement || !token) return
    loadManagementData()
  }, [isManagement, token])

  const loadManagementData = async () => {
    setLoading(true)
    setLoadError('')

    try {
      const allUsersResponse = await userService.getAllUsers(token)
      const allUsers = Array.isArray(allUsersResponse) ? allUsersResponse : []

      const usersById = Object.fromEntries(allUsers.map((user) => [user.id, user]))
      const clientUsers = allUsers.filter((user) =>
        !(user.roles || []).includes('Professional') &&
        !(user.roles || []).includes('Doctor') &&
        !(user.roles || []).includes('Admin') &&
        !(user.roles || []).includes('Management')
      )

      const ordersByClient = await Promise.all(
        clientUsers.map(async (client) => {
          try {
            const orders = await appointmentService.getOrdersByClient(client.id, token)
            return Array.isArray(orders) ? orders : []
          } catch {
            return []
          }
        })
      )

      const flattenedOrders = ordersByClient
        .flat()
        .map((order) => {
          const client = usersById[order.clientId]
          const doctor = usersById[order.professionalId]
          const scheduled = order.scheduledDateTime ? new Date(order.scheduledDateTime) : null

          return {
            id: order.id,
            clientName: `${client?.firstName || ''} ${client?.lastName || ''}`.trim() || client?.userName || 'Client',
            clientAvatar: client?.avatarUrl || null,
            serviceType: order.title || 'General consultation',
            timing: scheduled
              ? `${scheduled.toLocaleDateString()} ${scheduled.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
              : '-',
            status: statusText[order.status] || 'Pending',
            doctorName: `${doctor?.firstName || ''} ${doctor?.lastName || ''}`.trim() || doctor?.userName || 'Doctor',
            scheduledAt: scheduled ? scheduled.getTime() : 0,
          }
        })
        .sort((left, right) => (right.scheduledAt || 0) - (left.scheduledAt || 0))

      setClientOrders(flattenedOrders)

      const professionals = await appointmentService.getProfessionals(token)
      const professionalList = Array.isArray(professionals) ? professionals : []

      const schedules = await Promise.all(
        professionalList.map(async (professional) => {
          const doctorUser = usersById[professional.userId]
          let availabilities = []

          try {
            const data = await appointmentService.getAvailabilitiesByProfessional(professional.id, token)
            availabilities = Array.isArray(data) ? data : []
          } catch {
            availabilities = []
          }

          return {
            id: professional.id,
            doctorName: `${doctorUser?.firstName || ''} ${doctorUser?.lastName || ''}`.trim() || doctorUser?.userName || 'Doctor',
            doctorAvatar: doctorUser?.avatarUrl || null,
            schedules: availabilities.map((item) => ({
              day: item.dayOfWeek,
              slot: `${String(item.startTime).slice(0, 5)}-${String(item.endTime).slice(0, 5)}`,
            })),
          }
        })
      )

      setDoctorSchedules(schedules)
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
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center gap-2 mb-4">
                <Users size={18} className="text-primary-dark" />
                <h3 className="text-lg font-semibold text-text-primary">Clients Appointments</h3>
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
                    {clientOrders.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="py-6 text-center text-text-secondary">No orders found</td>
                      </tr>
                    ) : (
                      clientOrders.map((order) => (
                        <tr key={order.id} className="border-b border-gray-100 text-sm">
                          <td className="py-3 pr-2">
                            <div className="flex items-center gap-2">
                              <Avatar src={order.clientAvatar} alt={order.clientName} size={34} />
                              <span className="text-text-primary font-medium">{order.clientName}</span>
                            </div>
                          </td>
                          <td className="py-3 pr-2 text-text-primary">{order.serviceType}</td>
                          <td className="py-3 pr-2 text-text-secondary">{order.doctorName}</td>
                          <td className="py-3 pr-2 text-text-secondary">{order.timing}</td>
                          <td className="py-3 pr-2">
                            <span className="px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-700">{order.status}</span>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center gap-2 mb-4">
                <CalendarCheck size={18} className="text-primary-dark" />
                <h3 className="text-lg font-semibold text-text-primary">Doctors Availability Slots</h3>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full min-w-[900px]">
                  <thead>
                    <tr className="border-b border-gray-200 text-sm text-text-secondary">
                      <th className="text-left py-2 pr-2">Doctor</th>
                      <th className="text-left py-2 pr-2">Available Slots</th>
                    </tr>
                  </thead>
                  <tbody>
                    {doctorSchedules.length === 0 ? (
                      <tr>
                        <td colSpan={2} className="py-6 text-center text-text-secondary">No doctors found</td>
                      </tr>
                    ) : (
                      doctorSchedules.map((doctor) => (
                        <tr key={doctor.id} className="border-b border-gray-100 text-sm align-top">
                          <td className="py-3 pr-2">
                            <div className="flex items-center gap-2">
                              <Avatar src={doctor.doctorAvatar} alt={doctor.doctorName} size={34} />
                              <span className="text-text-primary font-medium">{doctor.doctorName}</span>
                            </div>
                          </td>
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
        </div>
      )}
    </MainContent>
  )
}
