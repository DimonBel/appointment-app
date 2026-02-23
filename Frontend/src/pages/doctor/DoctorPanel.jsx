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
import { Users, Search, ChevronLeft, ChevronRight, Eye, Stethoscope, AlertCircle, SortAsc, SortDesc } from 'lucide-react'

const ITEMS_PER_PAGE = 10

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

  useEffect(() => {
    if (!isDoctor) return
    loadClients()
  }, [isDoctor, token])

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
          const orders = await appointmentService.getOrdersByClient(client.id, token)
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
    </MainContent>
  )
}