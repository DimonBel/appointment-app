import React from 'react';
import type { Workflow } from '../types/api';
import { Users, UserCheck, Settings, Circle } from 'lucide-react';

interface WorkflowVisualizationProps {
  workflows: Workflow[];
}

const WorkflowVisualization: React.FC<WorkflowVisualizationProps> = ({ workflows }) => {
  const getWorkflowIcon = (workflowId: string) => {
    switch (workflowId) {
      case 'patient-booking':
        return <Users className="w-6 h-6 text-blue-600" />;
      case 'doctor-management':
        return <UserCheck className="w-6 h-6 text-green-600" />;
      case 'admin-slot-management':
        return <Settings className="w-6 h-6 text-purple-600" />;
      default:
        return <Circle className="w-6 h-6 text-gray-600" />;
    }
  };

  const getMethodColor = (method: string) => {
    switch (method.toUpperCase()) {
      case 'GET':
        return 'badge-success';
      case 'POST':
        return 'badge-info';
      case 'PUT':
        return 'badge-warning';
      case 'DELETE':
        return 'badge-error';
      case 'PUBLISH':
        return 'badge-primary';
      default:
        return 'badge';
    }
  };

  return (
    <div className="bg-white rounded-xl shadow-lg p-8 border" style={{ borderColor: 'var(--color-border-light)' }}>
      <div className="flex items-center space-x-3 mb-8">
        <div className="w-12 h-12 rounded-xl flex items-center justify-center"
             style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
          <svg className="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </div>
        <div>
          <h2 className="text-2xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
            API Workflows
          </h2>
          <p className="text-sm mt-0.5" style={{ color: 'var(--color-text-secondary)' }}>
            Visualize service interactions and data flow
          </p>
        </div>
      </div>
      
      <div className="space-y-8">
        {workflows.map((workflow) => (
          <div key={workflow.id} 
               className="border rounded-xl p-6 bg-white"
               style={{ borderColor: 'var(--color-border-light)', boxShadow: 'var(--shadow-md)' }}>
            <div className="flex items-center space-x-3 mb-6">
              {getWorkflowIcon(workflow.id)}
              <div>
                <h3 className="text-xl font-semibold" style={{ color: 'var(--color-text-primary)' }}>
                  {workflow.name}
                </h3>
                <p className="text-sm mt-1" style={{ color: 'var(--color-text-secondary)' }}>
                  {workflow.description}
                </p>
              </div>
            </div>

            <div className="relative">
              {/* Connection lines */}
              <svg className="absolute inset-0 w-full h-full pointer-events-none" style={{ zIndex: 1 }}>
                {workflow.connections.map((connection, index) => {
                  const fromStep = workflow.steps.find(s => s.id === connection.from);
                  const toStep = workflow.steps.find(s => s.id === connection.to);
                  
                  if (!fromStep || !toStep) return null;
                  
                  const startX = fromStep.position.x + 150;
                  const startY = fromStep.position.y + 40;
                  const endX = toStep.position.x - 10;
                  const endY = toStep.position.y + 40;
                  
                  return (
                    <g key={index}>
                      <line
                        x1={startX}
                        y1={startY}
                        x2={endX}
                        y2={endY}
                        stroke="#C7D2FE"
                        strokeWidth="3"
                        markerEnd="url(#arrowhead)"
                      />
                    </g>
                  );
                })}
                <defs>
                  <marker
                    id="arrowhead"
                    markerWidth="10"
                    markerHeight="7"
                    refX="9"
                    refY="3.5"
                    orient="auto"
                  >
                    <polygon
                      points="0 0, 10 3.5, 0 7"
                      fill="#C7D2FE"
                    />
                  </marker>
                </defs>
              </svg>

              {/* Workflow steps */}
              <div className="relative" style={{ zIndex: 2 }}>
                {workflow.steps.map((step, index) => (
                  <div
                    key={step.id}
                    className="absolute bg-white border-2 rounded-xl p-5 shadow-lg hover:shadow-xl transition-all"
                    style={{
                      left: `${step.position.x}px`,
                      top: `${step.position.y}px`,
                      width: '300px',
                      borderColor: 'var(--color-border)'
                    }}
                  >
                    <div className="flex items-center justify-between mb-3">
                      <span className={`badge ${getMethodColor(step.method)}`}>
                        {step.method}
                      </span>
                      <span className="badge badge-primary text-xs">
                        {step.service}
                      </span>
                    </div>
                    
                    <h4 className="font-semibold text-sm mb-2" style={{ color: 'var(--color-text-primary)' }}>
                      {step.name}
                    </h4>
                    <p className="text-xs mb-3" style={{ color: 'var(--color-text-secondary)' }}>
                      {step.description}
                    </p>
                    
                    <div className="pt-3 border-t" style={{ borderColor: 'var(--color-border-light)' }}>
                      <code className="text-xs px-3 py-2 rounded-lg block overflow-x-auto"
                            style={{ 
                              backgroundColor: '#EFF6FF',
                              color: 'var(--color-primary)',
                              border: '1px solid #BFDBFE'
                            }}>
                        {step.endpoint}
                      </code>
                    </div>

                    <div className="mt-3 flex items-center justify-between">
                      <span className="text-xs font-medium" style={{ color: 'var(--color-text-tertiary)' }}>
                        Step {index + 1}
                      </span>
                      {step.endpoint.includes('RabbitMQ') && (
                        <span className="badge text-xs"
                              style={{ 
                                backgroundColor: '#F3E8FF',
                                color: '#6B21A8'
                              }}>
                          Async
                        </span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Workflow metadata */}
            <div className="mt-20 pt-5 border-t" style={{ borderColor: 'var(--color-border-light)' }}>
              <div className="flex items-center justify-between text-sm">
                <div className="flex items-center space-x-6" style={{ color: 'var(--color-text-secondary)' }}>
                  <div className="flex items-center space-x-2">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                    </svg>
                    <span>Steps: <strong>{workflow.steps.length}</strong></span>
                  </div>
                  <div className="flex items-center space-x-2">
                    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
                    </svg>
                    <span>Connections: <strong>{workflow.connections.length}</strong></span>
                  </div>
                </div>
                <div className="flex items-center space-x-2">
                  <div className="w-2 h-2 rounded-full" style={{ backgroundColor: 'var(--color-success)' }}></div>
                  <span className="text-xs font-medium" style={{ color: 'var(--color-success)' }}>Active</span>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Legend */}
      <div className="mt-8 p-6 rounded-xl" 
           style={{ 
             background: 'linear-gradient(135deg, #F3F4F6 0%, #E5E7EB 100%)',
             border: '1px solid var(--color-border)'
           }}>
        <h4 className="font-semibold text-base mb-4" style={{ color: 'var(--color-text-primary)' }}>
          Method Legend
        </h4>
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
          <div className="flex items-center space-x-2">
            <span className="badge badge-success">GET</span>
            <span className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>Read data</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="badge badge-info">POST</span>
            <span className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>Create data</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="badge badge-warning">PUT</span>
            <span className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>Update data</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="badge badge-error">DELETE</span>
            <span className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>Delete data</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="badge text-xs"
                  style={{ 
                    backgroundColor: '#F3E8FF',
                    color: '#6B21A8'
                  }}>PUBLISH</span>
            <span className="text-xs" style={{ color: 'var(--color-text-secondary)' }}>Message queue</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default WorkflowVisualization;