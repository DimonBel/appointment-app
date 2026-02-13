import React from 'react';
import type { ApiEndpoint } from '../types/api';
import { ArrowRight, Play, Eye } from 'lucide-react';

interface ApiEndpointProps {
  endpoint: ApiEndpoint;
  onTest: (endpoint: ApiEndpoint) => void;
}

const ApiEndpointCard: React.FC<ApiEndpointProps> = ({ endpoint, onTest }) => {
  const getMethodColor = (method: string) => {
    switch (method) {
      case 'GET':
        return 'badge-success';
      case 'POST':
        return 'badge-info';
      case 'PUT':
        return 'badge-warning';
      case 'DELETE':
        return 'badge-error';
      default:
        return 'badge';
    }
  };

  const getMethodIcon = (method: string) => {
    switch (method) {
      case 'GET':
        return <Eye className="w-4 h-4" />;
      case 'POST':
      case 'PUT':
      case 'DELETE':
        return <Play className="w-4 h-4" />;
      default:
        return <ArrowRight className="w-4 h-4" />;
    }
  };

  return (
    <div 
      className="border rounded-xl p-5 hover:shadow-lg transition-all duration-200 group bg-white"
      style={{ borderColor: 'var(--color-border-light)' }}
    >
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center space-x-2">
          <span className={`badge inline-flex items-center space-x-1 ${getMethodColor(endpoint.method)}`}>
            {getMethodIcon(endpoint.method)}
            <span>{endpoint.method}</span>
          </span>
          <span className="badge badge-primary text-xs">
            {endpoint.service}
          </span>
        </div>
        <button
          onClick={() => onTest(endpoint)}
          className="opacity-0 group-hover:opacity-100 transition-all btn btn-ghost p-2"
          title="Test endpoint"
        >
          <Play className="w-4 h-4" style={{ color: 'var(--color-primary)' }} />
        </button>
      </div>

      <div className="space-y-3">
        <div>
          <code className="text-sm font-mono px-3 py-2 rounded-lg block overflow-x-auto" 
                style={{ 
                  backgroundColor: 'var(--color-background-tertiary)',
                  color: 'var(--color-primary)',
                  border: '1px solid var(--color-border-light)'
                }}>
            {endpoint.route}
          </code>
        </div>

        <p className="text-sm leading-relaxed" style={{ color: 'var(--color-text-secondary)' }}>
          {endpoint.description}
        </p>

        {endpoint.request && (
          <div className="mt-4">
            <span className="text-xs font-semibold mb-2 block" style={{ color: 'var(--color-text-tertiary)' }}>
              REQUEST BODY:
            </span>
            <code className="block text-xs rounded-lg p-3 overflow-x-auto"
                  style={{ 
                    backgroundColor: '#EFF6FF',
                    border: '1px solid #BFDBFE',
                    color: 'var(--color-text-primary)'
                  }}>
              {typeof endpoint.request === 'string' ? endpoint.request : JSON.stringify(endpoint.request, null, 2)}
            </code>
          </div>
        )}

        {endpoint.response && (
          <div className="mt-3">
            <span className="text-xs font-semibold mb-2 block" style={{ color: 'var(--color-text-tertiary)' }}>
              RESPONSE:
            </span>
            <code className="block text-xs rounded-lg p-3"
                  style={{ 
                    backgroundColor: '#D1FAE5',
                    border: '1px solid #A7F3D0',
                    color: 'var(--color-text-primary)'
                  }}>
              {endpoint.response}
            </code>
          </div>
        )}

        {endpoint.parameters && endpoint.parameters.length > 0 && (
          <div className="mt-3">
            <span className="text-xs font-semibold mb-2 block" style={{ color: 'var(--color-text-tertiary)' }}>
              PARAMETERS:
            </span>
            <div className="flex flex-wrap gap-2 mt-2">
              {endpoint.parameters.map((param, index) => (
                <span 
                  key={index} 
                  className="text-xs px-3 py-1.5 rounded-lg font-medium"
                  style={{ 
                    backgroundColor: '#F3E8FF',
                    color: '#6B21A8',
                    border: '1px solid #E9D5FF'
                  }}
                >
                  {param.name}: {param.type} {param.required && '*'}
                </span>
              ))}
            </div>
          </div>
        )}

        <div className="flex items-center justify-between mt-4 pt-4 border-t" 
             style={{ borderColor: 'var(--color-border-light)' }}>
          <span className="text-xs font-medium" style={{ color: 'var(--color-text-tertiary)' }}>
            Port: <span style={{ color: 'var(--color-primary)' }}>{endpoint.port}</span>
          </span>
          <a
            href={`http://localhost:${endpoint.port}/swagger`}
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs font-medium hover:underline"
            style={{ color: 'var(--color-primary)' }}
          >
            Open in Swagger →
          </a>
        </div>
      </div>
    </div>
  );
};

interface ApiEndpointsListProps {
  endpoints: ApiEndpoint[];
  onTestEndpoint: (endpoint: ApiEndpoint) => void;
}

const ApiEndpointsList: React.FC<ApiEndpointsListProps> = ({ endpoints, onTestEndpoint }) => {
  const groupedEndpoints = endpoints.reduce((acc, endpoint) => {
    if (!acc[endpoint.service]) {
      acc[endpoint.service] = [];
    }
    acc[endpoint.service].push(endpoint);
    return acc;
  }, {} as Record<string, ApiEndpoint[]>);

  const serviceColors: Record<string, string> = {
    'DoctorAvailability': 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    'AppointmentBooking': 'linear-gradient(135deg, #f093fb 0%, #f5576c 100%)',
    'DoctorAppointmentManagement': 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)'
  };

  return (
    <div className="bg-white rounded-xl shadow-lg p-8 border" style={{ borderColor: 'var(--color-border-light)' }}>
      <div className="flex items-center space-x-3 mb-8">
        <div className="w-12 h-12 rounded-xl flex items-center justify-center"
             style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
          <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 9l3 3-3 3m5 0h3M5 20h14a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
        </div>
        <div>
          <h2 className="text-2xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
            API Endpoints
          </h2>
          <p className="text-sm mt-0.5" style={{ color: 'var(--color-text-secondary)' }}>
            Explore and test all available endpoints
          </p>
        </div>
      </div>
      
      {Object.entries(groupedEndpoints).map(([serviceName, serviceEndpoints], index) => (
        <div key={serviceName} className={index > 0 ? 'mt-10' : ''}>
          <div className="flex items-center space-x-3 mb-5">
            <div 
              className="w-10 h-10 rounded-lg flex items-center justify-center"
              style={{ background: serviceColors[serviceName] || 'var(--color-primary)' }}
            >
              <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 12h14M5 12a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v4a2 2 0 01-2 2M5 12a2 2 0 00-2 2v4a2 2 0 002 2h14a2 2 0 002-2v-4a2 2 0 00-2-2m-2-4h.01M17 16h.01" />
              </svg>
            </div>
            <div>
              <h3 className="text-xl font-semibold" style={{ color: 'var(--color-text-primary)' }}>
                {serviceName}
              </h3>
              <p className="text-sm" style={{ color: 'var(--color-text-secondary)' }}>
                {serviceEndpoints.length} endpoint{serviceEndpoints.length !== 1 ? 's' : ''} available
              </p>
            </div>
          </div>
          
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-1">
            {serviceEndpoints.map((endpoint) => (
              <ApiEndpointCard
                key={endpoint.id}
                endpoint={endpoint}
                onTest={onTestEndpoint}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
};

export default ApiEndpointsList;