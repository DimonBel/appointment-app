import React from 'react';
import { Server, Activity, AlertCircle, CheckCircle, XCircle } from 'lucide-react';

interface Service {
  name: string;
  port: number;
  baseUrl: string;
  description: string;
  status: 'online' | 'offline' | 'unknown';
}

interface ServiceStatusProps {
  services: Service[];
  onRefresh: () => void;
}

const ServiceStatus: React.FC<ServiceStatusProps> = ({ services, onRefresh }) => {
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'online':
        return <CheckCircle className="w-6 h-6 text-green-500" />;
      case 'offline':
        return <XCircle className="w-6 h-6 text-red-500" />;
      default:
        return <AlertCircle className="w-6 h-6 text-yellow-500" />;
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'online':
        return 'badge badge-success';
      case 'offline':
        return 'badge badge-error';
      default:
        return 'badge badge-warning';
    }
  };

  const getStatusBackground = (status: string) => {
    switch (status) {
      case 'online':
        return 'linear-gradient(135deg, #D1FAE5 0%, #A7F3D0 100%)';
      case 'offline':
        return 'linear-gradient(135deg, #FEE2E2 0%, #FECACA 100%)';
      default:
        return 'linear-gradient(135deg, #FEF3C7 0%, #FDE68A 100%)';
    }
  };

  return (
    <div className="bg-white rounded-xl p-8 shadow-lg border" style={{ borderColor: 'var(--color-border-light)' }}>
      <div className="flex justify-between items-center mb-8">
        <div className="flex items-center space-x-3">
          <div className="w-12 h-12 rounded-xl flex items-center justify-center"
               style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
            <Server className="w-6 h-6 text-white" />
          </div>
          <div>
            <h2 className="text-2xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
              Service Status
            </h2>
            <p className="text-sm mt-0.5" style={{ color: 'var(--color-text-secondary)' }}>
              Monitor all microservices health
            </p>
          </div>
        </div>
        <button
          onClick={onRefresh}
          className="btn btn-primary flex items-center space-x-2 px-5 py-3"
        >
          <Activity className="w-5 h-5" />
          <span>Refresh Status</span>
        </button>
      </div>

      <div className="grid gap-5">
        {services.map((service) => (
          <div 
            key={service.name} 
            className="border rounded-xl p-5 transition-all hover:shadow-md"
            style={{ 
              borderColor: 'var(--color-border-light)',
              background: 'white'
            }}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-4 flex-1">
                <div 
                  className="w-16 h-16 rounded-xl flex items-center justify-center"
                  style={{ background: getStatusBackground(service.status) }}
                >
                  {getStatusIcon(service.status)}
                </div>
                <div className="flex-1">
                  <div className="flex items-center space-x-3 mb-2">
                    <h3 className="text-lg font-semibold" style={{ color: 'var(--color-text-primary)' }}>
                      {service.name}
                    </h3>
                    <span className={getStatusBadge(service.status)}>
                      {service.status.toUpperCase()}
                    </span>
                  </div>
                  <p className="text-sm mb-3" style={{ color: 'var(--color-text-secondary)' }}>
                    {service.description}
                  </p>
                  <div className="flex items-center space-x-5 text-sm">
                    <div className="flex items-center space-x-2">
                      <span style={{ color: 'var(--color-text-tertiary)' }}>Port:</span>
                      <code className="px-2 py-1 rounded text-xs font-mono" 
                            style={{ 
                              backgroundColor: 'var(--color-background-tertiary)',
                              color: 'var(--color-primary)'
                            }}>
                        {service.port}
                      </code>
                    </div>
                    <span style={{ color: 'var(--color-text-tertiary)' }}>•</span>
                    <div className="flex items-center space-x-2">
                      <span style={{ color: 'var(--color-text-tertiary)' }}>URL:</span>
                      <code className="px-2 py-1 rounded text-xs font-mono" 
                            style={{ 
                              backgroundColor: 'var(--color-background-tertiary)',
                              color: 'var(--color-text-secondary)'
                            }}>
                        {service.baseUrl}
                      </code>
                    </div>
                  </div>
                </div>
              </div>
              <div className="flex flex-col items-end space-y-3">
                <a
                  href={`${service.baseUrl}/swagger`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="btn btn-secondary text-sm px-4 py-2"
                  style={{ textDecoration: 'none' }}
                >
                  <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                  </svg>
                  <span>View Swagger</span>
                </a>
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="mt-8 p-6 rounded-xl" 
           style={{ 
             background: 'linear-gradient(135deg, #E0E7FF 0%, #C7D2FE 100%)',
             border: '1px solid var(--color-primary-light)'
           }}>
        <h4 className="font-semibold text-lg mb-3" style={{ color: 'var(--color-primary)' }}>
          📚 API Documentation
        </h4>
        <div className="space-y-2 text-sm" style={{ color: 'var(--color-text-primary)' }}>
          <div className="flex items-start space-x-2">
            <span>✓</span>
            <p>All services are configured with comprehensive Swagger/OpenAPI documentation</p>
          </div>
          <div className="flex items-start space-x-2">
            <span>✓</span>
            <p>Click "View Swagger" to explore detailed API documentation and test endpoints</p>
          </div>
          <div className="flex items-start space-x-2">
            <span>✓</span>
            <p>Services run on different ports following microservice architecture best practices</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ServiceStatus;