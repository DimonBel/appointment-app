import { useState, useEffect } from 'react'
import './App.css'
import './index.css'
import ServiceStatus from './components/ServiceStatus'
import ApiEndpointsList from './components/ApiEndpointsList'
import WorkflowVisualization from './components/WorkflowVisualization'
import ApiTester from './components/ApiTester'
import DataModelVisualization from './components/DataModelVisualization'
import { SERVICES, API_ENDPOINTS, WORKFLOWS } from './data/apiConfig'
import type { ApiEndpoint } from './types/api'
import apiService from './services/apiService'

function App() {
  const [services, setServices] = useState(SERVICES)
  const [selectedEndpoint, setSelectedEndpoint] = useState<ApiEndpoint | null>(null)
  const [activeTab, setActiveTab] = useState<'status' | 'endpoints' | 'workflows' | 'models'>('status')

  useEffect(() => {
    checkAllServicesHealth()
  }, [])

  const checkAllServicesHealth = async () => {
    const updatedServices = await Promise.all(
      services.map(async (service) => {
        const isOnline = await apiService.checkServiceHealth(service.name as 'DoctorAvailability' | 'DoctorAppointmentManagement' | 'AppointmentBooking')
        return {
          ...service,
          status: isOnline ? 'online' as const : 'offline' as const
        }
      })
    )
    setServices(updatedServices)
  }

  const handleTestEndpoint = async (endpoint: ApiEndpoint) => {
    setSelectedEndpoint(endpoint)
  }

  const handleRunTest = async (endpoint: ApiEndpoint) => {
    try {
      const result = await apiService.testEndpoint(
        endpoint.service as 'DoctorAvailability' | 'DoctorAppointmentManagement' | 'AppointmentBooking',
        endpoint.route,
        endpoint.method
      )
      return {
        ...result,
        timestamp: new Date().toISOString()
      }
    } catch (error: any) {
      return {
        success: false,
        error: error.message || 'Unknown error occurred',
        timestamp: new Date().toISOString()
      }
    }
  }

  return (
    <div className="min-h-screen" style={{ backgroundColor: 'var(--color-background-secondary)' }}>
      {/* Header */}
      <header className="bg-white shadow-sm border-b" style={{ borderColor: 'var(--color-border-light)' }}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-20">
            <div className="flex items-center space-x-4">
              <div className="w-12 h-12 rounded-xl flex items-center justify-center" 
                   style={{ 
                     background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                     boxShadow: '0 4px 12px rgba(102, 126, 234, 0.3)'
                   }}>
                <svg className="w-7 h-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z" />
                </svg>
              </div>
              <div>
                <h1 className="text-2xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
                  Appointment API Dashboard
                </h1>
                <p className="text-sm mt-0.5" style={{ color: 'var(--color-text-tertiary)' }}>
                  Professional Healthcare Management System
                </p>
              </div>
            </div>
            <div className="flex items-center space-x-3 px-4 py-2 rounded-lg" 
                 style={{ backgroundColor: 'var(--color-background-tertiary)' }}>
              <div className="flex items-center space-x-2">
                <div className="w-2 h-2 rounded-full" style={{ backgroundColor: 'var(--color-success)' }}></div>
                <span className="text-sm font-medium" style={{ color: 'var(--color-text-secondary)' }}>
                  Microservices Architecture
                </span>
              </div>
              <span style={{ color: 'var(--color-text-tertiary)' }}>•</span>
              <span className="text-sm font-medium" style={{ color: 'var(--color-text-secondary)' }}>
                ASP.NET Core
              </span>
            </div>
          </div>
        </div>
      </header>

      {/* Navigation Tabs */}
      <div className="bg-white border-b" style={{ borderColor: 'var(--color-border-light)' }}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <nav className="flex space-x-2">
            {[
              { id: 'status', label: 'Service Status', icon: (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2m-2-4h.01M17 16h.01" />
                </svg>
              ) },
              { id: 'endpoints', label: 'API Endpoints', icon: (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 9l3 3-3 3m5 0h3M5 20h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
              ) },
              { id: 'workflows', label: 'Workflows', icon: (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
              ) },
              { id: 'models', label: 'Data Models', icon: (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
                </svg>
              ) }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center space-x-2 py-4 px-4 border-b-2 font-medium text-sm transition-all ${
                  activeTab === tab.id
                    ? 'border-current'
                    : 'border-transparent hover:bg-opacity-50'
                }`}
                style={{
                  color: activeTab === tab.id ? 'var(--color-primary)' : 'var(--color-text-secondary)',
                  backgroundColor: activeTab === tab.id ? 'var(--color-primary-light)' : 'transparent',
                  borderRadius: '0.5rem 0.5rem 0 0'
                }}
              >
                {tab.icon}
                <span>{tab.label}</span>
              </button>
            ))}
          </nav>
        </div>
      </div>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="space-y-6">
          {activeTab === 'status' && (
            <div className="fade-in">
              <ServiceStatus services={services} onRefresh={checkAllServicesHealth} />
            </div>
          )}
          
          {activeTab === 'endpoints' && (
            <div className="fade-in">
              <ApiEndpointsList 
                endpoints={API_ENDPOINTS} 
                onTestEndpoint={handleTestEndpoint}
              />
            </div>
          )}
          
          {activeTab === 'workflows' && (
            <div className="fade-in">
              <WorkflowVisualization workflows={WORKFLOWS} />
            </div>
          )}
          
          {activeTab === 'models' && (
            <div className="fade-in">
              <DataModelVisualization />
            </div>
          )}
        </div>
      </main>

      {/* API Tester Modal */}
      <ApiTester
        endpoint={selectedEndpoint}
        onClose={() => setSelectedEndpoint(null)}
        onTest={handleRunTest}
      />
    </div>
  )
}

export default App
