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
import { Users, FileText, Download, Eye, Search, Calendar, Clock, Mail, Phone, ChevronLeft, ChevronRight } from 'lucide-react'

const ITEMS_PER_PAGE = 10

export const DoctorPanel = () => {
  const navigate = useNavigate()
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)
  const isDoctor = currentUser?.roles?.includes('Doctor') || currentUser?.roles?.includes('Professional')

  const [loading, setLoading] = useState(true)
  const [clients, setClients] = useState([])
  const [selectedClient, setSelectedClient] = useState(null)
  const [clientOrders, setClientOrders] = useState([])
  const [clientDocuments, setClientDocuments] = useState([])
  const [currentPage, setCurrentPage] = useState(1)
  const [searchQuery, setSearchQuery] = useState('')
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    if (!isDoctor) return
    loadClients()
  }, [isDoctor, token])

  const loadClients = async () => {
    setLoading(true)
    setLoadError('')
    try {
      const data = await appointmentService.getClientsByProfessional(currentUser.id, token)
      setClients(Array.isArray(data) ? data : [])
    } catch (error) {
      console.error('Failed to load clients:', error)
      setClients([])
      setLoadError(error?.response?.data?.message || error?.message || 'Failed to load clients')
    } finally {
      setLoading(false)
    }
  }

  const handleSelectClient = async (client) => {
    setSelectedClient(client)
    loadClientData(client.id)
  }

  const loadClientData = async (clientId) => {
    try {
      // Load orders for this client
      const orders = await appointmentService.getOrdersByClient(clientId, token)
      setClientOrders(Array.isArray(orders) ? orders : [])

      // Load documents linked to orders for this client
      const documents = []
      for (const order of orders) {
        try {
          const orderDocs = await documentService.getDocumentsByLinkedEntity('Order', order.id, token)
          documents.push(...orderDocs)
        } catch (err) {
          console.error(`Failed to load documents for order ${order.id}:`, err)
        }
      }
      setClientDocuments(documents)
    } catch (error) {
      console.error('Failed to load client data:', error)
      setClientOrders([])
      setClientDocuments([])
    }
  }

  const handleDownload = async (document) => {
    try {
      await documentService.downloadAndSave(document.id, document.originalFileName, token)
    } catch (error) {
      console.error('Failed to download document:', error)
      alert('Failed to download document')
    }
  }

  const handlePreview = (document) => {
    console.log('Previewing document with data:', document)
    console.log('Document linkedEntityType:', document.linkedEntityType)
    navigate('/document-preview', { state: { document, returnUrl: '/doctor-panel' } })
  }

  const filteredClients = clients.filter((client) => {
    if (!searchQuery) return true
    const query = searchQuery.toLowerCase()
    return (
      client.email?.toLowerCase().includes(query) ||
      client.userName?.toLowerCase().includes(query) ||
      `${client.firstName} ${client.lastName}`.toLowerCase().includes(query)
    )
  })

  const totalPages = Math.ceil(filteredClients.length / ITEMS_PER_PAGE)
  const paginatedClients = filteredClients.slice(
    (currentPage - 1) * ITEMS_PER_PAGE,
    currentPage * ITEMS_PER_PAGE
  )

  const getStatusColor = (status) => {
    switch (status) {
      case 0: return 'bg-yellow-100 text-yellow-700' // Requested
      case 1: return 'bg-green-100 text-green-700' // Approved
      case 2: return 'bg-red-100 text-red-700' // Declined
      case 3: return 'bg-gray-100 text-gray-700' // Cancelled
      case 4: return 'bg-blue-100 text-blue-700' // Completed
      case 5: return 'bg-orange-100 text-orange-700' // NoShow
      default: return 'bg-gray-100 text-gray-700'
    }
  }

  const getStatusLabel = (status) => {
    switch (status) {
      case 0: return 'Pending'
      case 1: return 'Approved'
      case 2: return 'Declined'
      case 3: return 'Cancelled'
      case 4: return 'Completed'
      case 5: return 'No Show'
      default: return 'Unknown'
    }
  }

  if (!isDoctor) {
    return (
      <MainContent>
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <Users size={64} className="mx-auto text-red-500 mb-4" />
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
        subtitle="View your clients and their documents"
      />

      {loadError && (
        <div className="mb-6 p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          {loadError}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Clients List */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users size={18} />
                My Clients
              </CardTitle>
              <div className="mt-4 relative">
                <Search size={16} className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  placeholder="Search clients..."
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value)
                    setCurrentPage(1)
                  }}
                  className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
                />
              </div>
            </CardHeader>
            <CardContent>
              {loading ? (
                <div className="flex justify-center py-8">
                  <Loader size="lg" />
                </div>
              ) : (
                <>
                  <div className="space-y-2">
                    {paginatedClients.map((client) => (
                      <button
                        key={client.id}
                        onClick={() => handleSelectClient(client)}
                        className={`w-full p-3 rounded-lg text-left transition-colors ${
                          selectedClient?.id === client.id
                            ? 'bg-blue-50 border border-blue-200'
                            : 'hover:bg-gray-50 border border-transparent'
                        }`}
                      >
                        <div className="flex items-center gap-3">
                          <Avatar src={client.avatarUrl} alt={client.userName} size={40} />
                          <div className="flex-1 min-w-0">
                            <p className="font-medium text-text-primary truncate">
                              {client.firstName} {client.lastName}
                            </p>
                            <p className="text-sm text-text-secondary truncate">{client.email}</p>
                          </div>
                        </div>
                      </button>
                    ))}
                  </div>

                  {totalPages > 1 && (
                    <div className="flex items-center justify-between mt-4 pt-4 border-t border-gray-200">
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                        disabled={currentPage === 1}
                      >
                        <ChevronLeft size={16} />
                      </Button>
                      <span className="text-sm text-text-secondary">
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
                  )}

                  {!loading && filteredClients.length === 0 && (
                    <div className="text-center py-8 text-text-secondary">
                      {searchQuery ? 'No clients found matching your search' : 'No clients yet'}
                    </div>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Client Details */}
        <div className="lg:col-span-2">
          {selectedClient ? (
            <>
              {/* Client Info Card */}
              <Card className="mb-6">
                <CardHeader>
                  <CardTitle>Client Information</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex items-start gap-4">
                    <Avatar src={selectedClient.avatarUrl} alt={selectedClient.userName} size={64} />
                    <div className="flex-1">
                      <h3 className="text-xl font-semibold text-text-primary">
                        {selectedClient.firstName} {selectedClient.lastName}
                      </h3>
                      <p className="text-text-secondary">@{selectedClient.userName}</p>
                      <div className="flex flex-wrap gap-4 mt-3 text-sm">
                        {selectedClient.email && (
                          <div className="flex items-center gap-2 text-text-secondary">
                            <Mail size={14} />
                            {selectedClient.email}
                          </div>
                        )}
                        {selectedClient.phoneNumber && (
                          <div className="flex items-center gap-2 text-text-secondary">
                            <Phone size={14} />
                            {selectedClient.phoneNumber}
                          </div>
                        )}
                      </div>
                      <div className="mt-2 text-xs text-text-secondary">
                        Member since {new Date(selectedClient.createdAt).toLocaleDateString()}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Appointments */}
              <Card className="mb-6">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Calendar size={18} />
                    Appointments ({clientOrders.length})
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {clientOrders.length === 0 ? (
                    <div className="text-center py-6 text-text-secondary">No appointments yet</div>
                  ) : (
                    <div className="space-y-3">
                      {clientOrders.map((order) => (
                        <div key={order.id} className="p-4 border border-gray-200 rounded-lg">
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <h4 className="font-medium text-text-primary">{order.title || 'Appointment'}</h4>
                              {order.description && (
                                <p className="text-sm text-text-secondary mt-1">{order.description}</p>
                              )}
                              <div className="flex items-center gap-4 mt-2 text-sm text-text-secondary">
                                <div className="flex items-center gap-1">
                                  <Clock size={14} />
                                  {new Date(order.scheduledDateTime).toLocaleString()}
                                </div>
                                <span>{order.durationMinutes} min</span>
                              </div>
                            </div>
                            <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(order.status)}`}>
                              {getStatusLabel(order.status)}
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Documents */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <FileText size={18} />
                    Documents ({clientDocuments.length})
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {clientDocuments.length === 0 ? (
                    <div className="text-center py-6 text-text-secondary">No documents yet</div>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="w-full">
                        <thead>
                          <tr className="border-b border-gray-200 text-sm text-text-secondary">
                            <th className="text-left py-2">File</th>
                            <th className="text-left py-2">Size</th>
                            <th className="text-left py-2">Uploaded</th>
                            <th className="text-right py-2">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {clientDocuments.map((doc) => (
                            <tr key={doc.id} className="border-b border-gray-100 text-sm">
                              <td className="py-3">
                                <div className="flex items-center gap-2">
                                  <span className="text-xl">{documentService.getFileIcon(doc.contentType)}</span>
                                  <p className="text-text-primary font-medium truncate max-w-[200px]" title={doc.originalFileName}>
                                    {doc.originalFileName}
                                  </p>
                                </div>
                              </td>
                              <td className="py-3 text-text-secondary">
                                {documentService.formatFileSize(doc.fileSize)}
                              </td>
                              <td className="py-3 text-text-secondary">
                                {new Date(doc.createdAt).toLocaleDateString()}
                              </td>
                              <td className="py-3 text-right">
                                <div className="flex items-center justify-end gap-2">
                                  {documentService.canPreview(doc.contentType) && (
                                    <button
                                      onClick={() => handlePreview(doc)}
                                      className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors"
                                      title="Preview"
                                    >
                                      <Eye size={16} />
                                    </button>
                                  )}
                                  <button
                                    onClick={() => handleDownload(doc)}
                                    className="p-1.5 hover:bg-gray-100 rounded-lg transition-colors"
                                    title="Download"
                                  >
                                    <Download size={16} />
                                  </button>
                                </div>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </CardContent>
              </Card>
            </>
          ) : (
            <Card>
              <CardContent className="p-12 text-center">
                <Users size={64} className="mx-auto text-gray-300 mb-4" />
                <h3 className="text-lg font-medium text-text-primary mb-2">Select a Client</h3>
                <p className="text-text-secondary">Choose a client from the list to view their information and documents</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </MainContent>
  )
}