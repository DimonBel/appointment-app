import React, { useState } from 'react';
import type { ApiEndpoint } from '../types/api';
import { X, CheckCircle, XCircle, Copy, Play } from 'lucide-react';

interface ApiTestResult {
  success: boolean;
  data?: any;
  error?: string;
  timestamp: string;
}

interface ApiTesterProps {
  endpoint: ApiEndpoint | null;
  onClose: () => void;
  onTest: (endpoint: ApiEndpoint) => Promise<ApiTestResult>;
}

const ApiTester: React.FC<ApiTesterProps> = ({ endpoint, onClose, onTest }) => {
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<ApiTestResult | null>(null);
  const [requestBody, setRequestBody] = useState('');
  

  if (!endpoint) return null;

  const handleTest = async () => {
    setIsLoading(true);
    try {
      const testResult = await onTest(endpoint);
      setResult(testResult);
    } catch (error) {
      setResult({
        success: false,
        error: 'Failed to test endpoint',
        timestamp: new Date().toISOString()
      });
    } finally {
      setIsLoading(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

  const getExampleRequest = () => {
    if (endpoint.route.includes('DoctorSlot')) {
      return JSON.stringify({
        date: "2024-12-25T10:00:00",
        doctorId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        doctorName: "Dr. John Doe",
        cost: 150.00
      }, null, 2);
    }
    if (endpoint.route.includes('AppoimentBooking')) {
      return JSON.stringify({
        slotId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        patientId: "3fa85f64-5717-4562-b3fc-2c963f66afa7",
        patientName: "Jane Smith",
        doctorName: "Dr. John Doe"
      }, null, 2);
    }
    if (endpoint.route.includes('ChangeAppoinmentStatus')) {
      return JSON.stringify({
        slotId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        statusId: 1
      }, null, 2);
    }
    return '{}';
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-50 backdrop-blur-sm"
         style={{ display: endpoint ? 'flex' : 'none' }}>
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-y-auto m-4"
           style={{ border: '1px solid var(--color-border-light)' }}>
        <div className="flex items-center justify-between p-6 border-b sticky top-0 bg-white z-10 rounded-t-2xl"
             style={{ borderColor: 'var(--color-border-light)' }}>
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-lg flex items-center justify-center"
                 style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
              <Play className="w-5 h-5 text-white" />
            </div>
            <div>
              <h2 className="text-xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
                API Endpoint Tester
              </h2>
              <p className="text-sm mt-0.5" style={{ color: 'var(--color-text-secondary)' }}>
                {endpoint?.description}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
          >
            <X className="w-5 h-5" style={{ color: 'var(--color-text-tertiary)' }} />
          </button>
        </div>

        <div className="p-6 space-y-6">
          {/* Endpoint Info */}
          <div className="rounded-xl p-5"
               style={{ 
                 background: 'linear-gradient(135deg, #F3F4F6 0%, #E5E7EB 100%)',
                 border: '1px solid var(--color-border)'
               }}>
            <div className="flex items-center space-x-4 flex-wrap gap-2">
              <span className={`badge ${
                endpoint?.method === 'GET' ? 'badge-success' :
                endpoint?.method === 'POST' ? 'badge-info' :
                endpoint?.method === 'PUT' ? 'badge-warning' :
                'badge'
              }`}>
                {endpoint?.method}
              </span>
              <code className="text-sm px-4 py-2 rounded-lg font-mono bg-white flex-1"
                    style={{ 
                      border: '1px solid var(--color-border)',
                      color: 'var(--color-primary)',
                      minWidth: '200px'
                    }}>
                http://localhost:{endpoint?.port}{endpoint?.route}
              </code>
              <span className="badge badge-primary">{endpoint?.service}</span>
            </div>
          </div>

          {/* Request Configuration */}
          {endpoint?.method !== 'GET' && (
            <div>
              <h3 className="text-sm font-semibold mb-3 flex items-center space-x-2"
                  style={{ color: 'var(--color-text-primary)' }}>
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
                <span>Request Body</span>
              </h3>
              <div className="space-y-3">
                <textarea
                  value={requestBody}
                  onChange={(e) => setRequestBody(e.target.value)}
                  placeholder="Enter JSON request body..."
                  className="w-full h-40 p-4 border rounded-xl font-mono text-sm focus:ring-2 focus:ring-offset-2 resize-none"
                  style={{ 
                    borderColor: 'var(--color-border)',
                    backgroundColor: '#FAFAFA'
                  }}
                  defaultValue={getExampleRequest()}
                />
                <button
                  onClick={() => setRequestBody(getExampleRequest())}
                  className="btn btn-secondary text-sm"
                >
                  📋 Load Example Request
                </button>
              </div>
            </div>
          )}

          {/* Query Parameters */}
          {endpoint?.parameters && endpoint.parameters.length > 0 && (
            <div>
              <h3 className="text-sm font-semibold mb-3 flex items-center space-x-2"
                  style={{ color: 'var(--color-text-primary)' }}>
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
                </svg>
                <span>Query Parameters</span>
              </h3>
              <div className="space-y-3">
                {endpoint.parameters.map((param, index) => (
                  <div key={index} className="flex items-center space-x-3">
                    <label className="text-sm font-medium w-28"
                           style={{ color: 'var(--color-text-secondary)' }}>
                      {param.name}:
                    </label>
                    <input
                      type="text"
                      placeholder={`Enter ${param.name}...`}
                      className="flex-1 p-3 border rounded-lg text-sm focus:ring-2"
                      style={{ borderColor: 'var(--color-border)' }}
                      defaultValue={param.type === 'Guid' ? '3fa85f64-5717-4562-b3fc-2c963f66afa6' : ''}
                    />
                    <span className="badge text-xs">{param.type}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Test Button */}
          <div className="flex items-center space-x-3 pt-4">
            <button
              onClick={handleTest}
              disabled={isLoading}
              className="btn btn-primary flex items-center space-x-2 px-6 py-3 text-base"
            >
              <Play className="w-5 h-5" />
              <span>{isLoading ? 'Testing...' : 'Run Test'}</span>
            </button>
            
            {result && (
              <button
                onClick={() => setResult(null)}
                className="btn btn-secondary px-6 py-3"
              >
                Clear Result
              </button>
            )}
          </div>

          {/* Test Result */}
          {result && (
            <div className="border rounded-xl overflow-hidden shadow-md"
                 style={{ borderColor: 'var(--color-border)' }}>
              <div className={`flex items-center space-x-3 px-5 py-4 border-b`}
                   style={{
                     background: result.success ? 
                       'linear-gradient(135deg, #D1FAE5 0%, #A7F3D0 100%)' : 
                       'linear-gradient(135deg, #FEE2E2 0%, #FECACA 100%)',
                     borderColor: 'var(--color-border)'
                   }}>
                {result.success ? (
                  <CheckCircle className="w-6 h-6 text-green-600" />
                ) : (
                  <XCircle className="w-6 h-6 text-red-600" />
                )}
                <span className={`font-semibold text-base ${
                  result.success ? 'text-green-800' : 'text-red-800'
                }`}>
                  {result.success ? 'Test Successful' : 'Test Failed'}
                </span>
                <span className="text-sm ml-auto"
                      style={{ color: result.success ? '#065F46' : '#991B1B' }}>
                  {new Date(result.timestamp).toLocaleTimeString()}
                </span>
              </div>
              
              <div className="p-5 bg-white">
                {result.success && result.data && (
                  <div>
                    <div className="flex items-center justify-between mb-3">
                      <h4 className="text-sm font-semibold" style={{ color: 'var(--color-text-primary)' }}>
                        Response Data
                      </h4>
                      <button
                        onClick={() => copyToClipboard(JSON.stringify(result.data, null, 2))}
                        className="btn btn-ghost text-sm px-3 py-1.5 flex items-center space-x-1"
                        style={{ color: 'var(--color-primary)' }}
                      >
                        <Copy className="w-4 h-4" />
                        <span>Copy</span>
                      </button>
                    </div>
                    <pre className="p-4 rounded-lg text-xs overflow-x-auto max-h-96 font-mono"
                         style={{ 
                           backgroundColor: '#FAFAFA',
                           border: '1px solid var(--color-border)',
                           color: 'var(--color-text-primary)'
                         }}>
                      {JSON.stringify(result.data, null, 2)}
                    </pre>
                  </div>
                )}
                
                {!result.success && result.error && (
                  <div>
                    <h4 className="text-sm font-semibold mb-3" style={{ color: 'var(--color-text-primary)' }}>
                      Error Message
                    </h4>
                    <div className="p-4 rounded-lg"
                         style={{ 
                           backgroundColor: '#FEE2E2',
                           border: '1px solid #FECACA'
                         }}>
                      <p className="text-sm text-red-800 font-mono">{result.error}</p>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default ApiTester;