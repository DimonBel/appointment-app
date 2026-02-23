import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { useNavigate, useParams } from 'react-router-dom'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Avatar } from '../../components/ui/Avatar'
import { Loader } from '../../components/ui/Loader'
import { appointmentService } from '../../services/appointmentService'
import documentService from '../../services/documentService'
import { FileText, Download, Eye, Calendar, Clock, Mail, Phone, X, File, UserCircle, AlertCircle, ArrowLeft } from 'lucide-react'

export const ClientDetail = () => {
  const navigate = useNavigate()
  const { clientId } = useParams()
  const token = useSelector((state) => state.auth.token)
  const currentUser = useSelector((state) => state.auth.user)
  const isDoctor = currentUser?.roles?.includes('Doctor') || currentUser?.roles?.includes('Professional')

  const [loading, setLoading] = useState(true)
  const [loadingClientData, setLoadingClientData] = useState(false)
  const [client, setClient] = useState(null)
  const [clientOrders, setClientOrders] = useState([])
  const [clientDocuments, setClientDocuments] = useState([])
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    if (!isDoctor || !clientId) return
    loadClient()
  }, [isDoctor, clientId, token])

  const loadClient = async () => {
    setLoading(true)
    setLoadError('')
    try {
      // Load all clients and find the specific one
      const allClients = await appointmentService.getClientsByProfessional(currentUser.id, token)
      const foundClient = Array.isArray(allClients) ? allClients.find(c => String(c.id) === String(clientId)) : null

      if (!foundClient) {
        setLoadError('Client not found')
        setClient(null)
      } else {
        setClient(foundClient)
        loadClientData(foundClient.id)
      }
    } catch (error) {
      console.error('Failed to load client:', error)
      setLoadError(error?.response?.data?.message || error?.message || 'Failed to load client')
      setClient(null)
    } finally {
      setLoading(false)
    }
  }

  const loadClientData = async (clientId) => {
    setLoadingClientData(true)
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
    } finally {
      setLoadingClientData(false)
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
    navigate('/document-preview', { state: { document, returnUrl: `/doctor-panel/client/${clientId}` } })
  }

  const getStatusColor = (status) => {
    switch (status) {
      case 0: return 'bg-yellow-100 text-yellow-700 border-yellow-200'
      case 1: return 'bg-green-100 text-green-700 border-green-200'
      case 2: return 'bg-red-100 text-red-700 border-red-200'
      case 3: return 'bg-gray-100 text-gray-700 border-gray-200'
      case 4: return 'bg-blue-100 text-blue-700 border-blue-200'
      case 5: return 'bg-orange-100 text-orange-700 border-orange-200'
      default: return 'bg-gray-100 text-gray-700 border-gray-200'
    }
  }

  const getStatusIcon = (status) => {
    switch (status) {
      case 0: return <Clock size={12} />
      case 1: return <Calendar size={12} />
      case 2: return <AlertCircle size={12} />
      case 3: return <X size={12} />
      case 4: return <FileText size={12} />
      case 5: return <AlertCircle size={12} />
      default: return null
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

  const getUpcomingAppointments = () => {
    const now = new Date()
    return clientOrders.filter(order => new Date(order.scheduledDateTime) > now)
  }

  const getPastAppointments = () => {
    const now = new Date()
    return clientOrders.filter(order => new Date(order.scheduledDateTime) <= now)
  }

  if (!isDoctor) {
    return (
      <MainContent>
        <div className="flex items-center justify-center h-96">
          <div className="text-center">
            <FileText size={64} className="mx-auto text-red-500 mb-4" />
            <h2 className="text-2xl font-semibold text-text-primary mb-2">Access Denied</h2>
            <p className="text-text-secondary">This page is only accessible to doctors and professionals.</p>
          </div>
        </div>
      </MainContent>
    )
  }

  return (
    <MainContent>
      {/* Back Button */}
      <Button
        variant="outline"
        size="sm"
        onClick={() => navigate('/doctor-panel')}
        className="mb-4"
      >
        <ArrowLeft size={16} className="mr-2" />
        Back to Clients
      </Button>

      <SectionHeader
        title="Client Details"
        subtitle={client ? `${client.firstName} ${client.lastName}` : ''}
      />

      {loadError && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-xl text-red-700 text-sm flex items-center gap-3">
          <AlertCircle size={18} />
          {loadError}
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-16">
          <Loader size="lg" />
        </div>
      ) : client ? (
        loadingClientData ? (
          <Card>
            <CardContent className="p-16 flex justify-center">
              <Loader size="lg" />
            </CardContent>
          </Card>
        ) : (
          <>
            {/* Client Info Card */}
            <Card className="mb-6 border-l-4 border-l-blue-500">
              <CardHeader className="pb-4">
                <CardTitle className="flex items-center gap-2 text-base">
                  <UserCircle size={18} />
                  Client Profile
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-col sm:flex-row items-start gap-5">
                  <Avatar src={client.avatarUrl} alt={client.userName} size={72} />
                  <div className="flex-1 w-full">
                    <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-3">
                      <div>
                        <h3 className="text-xl font-semibold text-text-primary">
                          {client.firstName} {client.lastName}
                        </h3>
                        <p className="text-sm text-text-secondary">@{client.userName}</p>
                      </div>
                      <div className="text-xs text-text-secondary bg-gray-100 px-3 py-1.5 rounded-full inline-flex items-center gap-1.5">
                        <Calendar size={12} />
                        Member since {new Date(client.createdAt).toLocaleDateString()}
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-4 text-sm">
                      {client.email && (
                        <a
                          href={`mailto:${client.email}`}
                          className="flex items-center gap-2 text-text-secondary hover:text-blue-600 transition-colors"
                        >
                          <Mail size={14} />
                          {client.email}
                        </a>
                      )}
                      {client.phoneNumber && (
                        <a
                          href={`tel:${client.phoneNumber}`}
                          className="flex items-center gap-2 text-text-secondary hover:text-blue-600 transition-colors"
                        >
                          <Phone size={14} />
                          {client.phoneNumber}
                        </a>
                      )}
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Quick Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
              <Card className="text-center p-4">
                <div className="text-2xl font-bold text-blue-600">{clientOrders.length}</div>
                <div className="text-xs text-text-secondary mt-1">Total Appointments</div>
              </Card>
              <Card className="text-center p-4">
                <div className="text-2xl font-bold text-green-600">{getUpcomingAppointments().length}</div>
                <div className="text-xs text-text-secondary mt-1">Upcoming</div>
              </Card>
              <Card className="text-center p-4">
                <div className="text-2xl font-bold text-gray-600">{getPastAppointments().length}</div>
                <div className="text-xs text-text-secondary mt-1">Completed</div>
              </Card>
              <Card className="text-center p-4">
                <div className="text-2xl font-bold text-purple-600">{clientDocuments.length}</div>
                <div className="text-xs text-text-secondary mt-1">Documents</div>
              </Card>
            </div>

            {/* Upcoming Appointments - Priority */}
            {getUpcomingAppointments().length > 0 && (
              <Card className="mb-6 border-l-4 border-l-yellow-500">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-base">
                    <Calendar size={18} className="text-yellow-600" />
                    Upcoming Appointments
                    <span className="ml-1 px-2 py-0.5 bg-yellow-100 text-yellow-700 text-xs rounded-full">
                      {getUpcomingAppointments().length}
                    </span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    {getUpcomingAppointments().map((order) => (
                      <div key={order.id} className="p-4 bg-yellow-50 border border-yellow-200 rounded-xl">
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex-1 min-w-0">
                            <h4 className="font-medium text-text-primary">{order.title || 'Appointment'}</h4>
                            {order.description && (
                              <p className="text-sm text-text-secondary mt-1 truncate">{order.description}</p>
                            )}
                            <div className="flex flex-wrap items-center gap-3 mt-2 text-sm text-text-secondary">
                              <div className="flex items-center gap-1.5">
                                <Clock size={14} />
                                {new Date(order.scheduledDateTime).toLocaleString()}
                              </div>
                              <span className="px-2 py-0.5 bg-yellow-100 text-yellow-700 text-xs rounded-full">
                                {order.durationMinutes} min
                              </span>
                            </div>
                          </div>
                          <span className={`px-2.5 py-1 rounded-full text-xs font-medium border flex items-center gap-1.5 shrink-0 ${getStatusColor(order.status)}`}>
                            {getStatusIcon(order.status)}
                            {getStatusLabel(order.status)}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Past Appointments */}
            {getPastAppointments().length > 0 && (
              <Card className="mb-6">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-base">
                    <FileText size={18} />
                    Appointment History
                    <span className="ml-1 px-2 py-0.5 bg-gray-100 text-gray-700 text-xs rounded-full">
                      {getPastAppointments().length}
                    </span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    {getPastAppointments().map((order) => (
                      <div key={order.id} className="p-3 border border-gray-200 rounded-xl hover:bg-gray-50 transition-colors">
                        <div className="flex items-start justify-between gap-3">
                          <div className="flex-1 min-w-0">
                            <h4 className="font-medium text-text-primary text-sm">{order.title || 'Appointment'}</h4>
                            <div className="flex items-center gap-3 mt-1.5 text-xs text-text-secondary">
                              <div className="flex items-center gap-1">
                                <Clock size={12} />
                                {new Date(order.scheduledDateTime).toLocaleDateString()}
                              </div>
                              <span>{order.durationMinutes} min</span>
                            </div>
                          </div>
                          <span className={`px-2 py-1 rounded-full text-xs font-medium border flex items-center gap-1 shrink-0 ${getStatusColor(order.status)}`}>
                            {getStatusIcon(order.status)}
                            {getStatusLabel(order.status)}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Documents */}
            {clientDocuments.length > 0 && (
              <Card className="border-l-4 border-l-purple-500">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2 text-base">
                    <File size={18} className="text-purple-600" />
                    Documents
                    <span className="ml-1 px-2 py-0.5 bg-purple-100 text-purple-700 text-xs rounded-full">
                      {clientDocuments.length}
                    </span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead>
                        <tr className="border-b border-gray-200 text-xs text-text-secondary uppercase tracking-wider">
                          <th className="text-left py-3 font-medium">File Name</th>
                          <th className="text-left py-3 font-medium">Size</th>
                          <th className="text-left py-3 font-medium">Uploaded</th>
                          <th className="text-right py-3 font-medium">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {clientDocuments.map((doc) => (
                          <tr key={doc.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors text-sm">
                            <td className="py-3">
                              <div className="flex items-center gap-2.5">
                                <span className="text-lg">{documentService.getFileIcon(doc.contentType)}</span>
                                <p className="text-text-primary font-medium truncate max-w-[180px]" title={doc.originalFileName}>
                                  {doc.originalFileName}
                                </p>
                              </div>
                            </td>
                            <td className="py-3 text-text-secondary text-xs">
                              {documentService.formatFileSize(doc.fileSize)}
                            </td>
                            <td className="py-3 text-text-secondary text-xs">
                              {new Date(doc.createdAt).toLocaleDateString()}
                            </td>
                            <td className="py-3 text-right">
                              <div className="flex items-center justify-end gap-1">
                                {documentService.canPreview(doc.contentType) && (
                                  <button
                                    onClick={() => handlePreview(doc)}
                                    className="p-2 hover:bg-blue-100 text-blue-600 rounded-lg transition-colors"
                                    title="Preview"
                                  >
                                    <Eye size={16} />
                                  </button>
                                )}
                                <button
                                  onClick={() => handleDownload(doc)}
                                  className="p-2 hover:bg-green-100 text-green-600 rounded-lg transition-colors"
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
                </CardContent>
              </Card>
            )}

            {getUpcomingAppointments().length === 0 && getPastAppointments().length === 0 && clientDocuments.length === 0 && (
              <Card>
                <CardContent className="p-12 text-center">
                  <FileText size={48} className="mx-auto text-gray-300 mb-3" />
                  <h3 className="text-base font-medium text-text-primary mb-2">No Data Yet</h3>
                  <p className="text-sm text-text-secondary">This client doesn't have any appointments or documents recorded.</p>
                </CardContent>
              </Card>
            )}
          </>
        )
      ) : (
        <Card>
          <CardContent className="p-16 text-center">
            <UserCircle size={64} className="mx-auto text-gray-300 mb-4" />
            <h3 className="text-lg font-medium text-text-primary mb-2">Client Not Found</h3>
            <p className="text-text-secondary mb-4">The client you're looking for doesn't exist or you don't have access.</p>
            <Button onClick={() => navigate('/doctor-panel')}>
              Back to Clients
            </Button>
          </CardContent>
        </Card>
      )}
    </MainContent>
  )
}