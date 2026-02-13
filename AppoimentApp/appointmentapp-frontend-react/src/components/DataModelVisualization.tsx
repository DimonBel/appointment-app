import React from 'react';
import { Database, ArrowRight, Users, Calendar } from 'lucide-react';

interface DataModelVisualizationProps {
  show?: boolean;
}

const DataModelVisualization: React.FC<DataModelVisualizationProps> = ({ show = true }) => {
  if (!show) return null;

  return (
    <div className="bg-white rounded-xl shadow-lg p-8 border" style={{ borderColor: 'var(--color-border-light)' }}>
      <div className="flex items-center space-x-3 mb-8">
        <div className="w-12 h-12 rounded-xl flex items-center justify-center"
             style={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)' }}>
          <Database className="w-6 h-6 text-white" />
        </div>
        <div>
          <h2 className="text-2xl font-bold" style={{ color: 'var(--color-text-primary)' }}>
            Data Model Relationships
          </h2>
          <p className="text-sm mt-0.5" style={{ color: 'var(--color-text-secondary)' }}>
            Database schema and entity relationships
          </p>
        </div>
      </div>

      <div className="space-y-8">
        {/* Entity Models */}
        <div>
          <h3 className="text-lg font-semibold mb-5" style={{ color: 'var(--color-text-primary)' }}>
            Core Entities
          </h3>
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            
            {/* Slot Entity */}
            <div className="border-2 rounded-xl p-5 transition-all hover:shadow-lg"
                 style={{ 
                   background: 'linear-gradient(135deg, #EFF6FF 0%, #DBEAFE 100%)',
                   borderColor: '#BFDBFE'
                 }}>
              <div className="flex items-center space-x-2 mb-4">
                <Calendar className="w-6 h-6 text-blue-600" />
                <h4 className="font-semibold text-lg text-blue-900">Slot</h4>
              </div>
              <div className="space-y-2 text-sm">
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-blue-100 text-blue-800 font-mono">Guid</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>Id</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-blue-100 text-blue-800 font-mono">DateTime</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>Date</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-blue-100 text-blue-800 font-mono">Guid</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>DoctorId</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-blue-100 text-blue-800 font-mono">string</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>DoctorName</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-blue-100 text-blue-800 font-mono">bool</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>IsReserved</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-blue-100 text-blue-800 font-mono">decimal</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>Cost</span>
                </div>
              </div>
              <div className="mt-4 pt-4 border-t border-blue-200">
                <span className="text-xs font-medium text-blue-700">Service: DoctorAvailability</span>
              </div>
            </div>

            {/* AppointmentBooking Entity */}
            <div className="border-2 rounded-xl p-5 transition-all hover:shadow-lg"
                 style={{ 
                   background: 'linear-gradient(135deg, #D1FAE5 0%, #A7F3D0 100%)',
                   borderColor: '#86EFAC'
                 }}>
              <div className="flex items-center space-x-2 mb-4">
                <Users className="w-6 h-6 text-green-600" />
                <h4 className="font-semibold text-lg text-green-900">AppointmentBooking</h4>
              </div>
              <div className="space-y-2 text-sm">
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-green-100 text-green-800 font-mono">Guid</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>Id</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-green-100 text-green-800 font-mono">DateTime</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>ReservedAt</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-green-100 text-green-800 font-mono">Guid</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>SlotId</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-green-100 text-green-800 font-mono">Guid</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>PatientId</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-green-100 text-green-800 font-mono">string</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>PatientName</span>
                </div>
                <div className="flex items-start space-x-2">
                  <span className="badge text-xs bg-green-100 text-green-800 font-mono">int?</span>
                  <span style={{ color: 'var(--color-text-primary)' }}>AppointmentStatus</span>
                </div>
              </div>
              <div className="mt-4 pt-4 border-t border-green-200">
                <span className="text-xs font-medium text-green-700">Service: AppointmentBooking</span>
              </div>
            </div>

            {/* Models Variations */}
            <div className="border-2 rounded-xl p-5 transition-all hover:shadow-lg"
                 style={{ 
                   background: 'linear-gradient(135deg, #FEF3C7 0%, #FDE68A 100%)',
                   borderColor: '#FCD34D'
                 }}>
              <div className="flex items-center space-x-2 mb-4">
                <Database className="w-6 h-6 text-yellow-600" />
                <h4 className="font-semibold text-lg text-yellow-900">SlotModel (Variations)</h4>
              </div>
              <div className="space-y-3 text-sm">
                <div>
                  <div className="text-xs font-semibold text-yellow-800 mb-2">DoctorAvailability:</div>
                  <div className="text-xs ml-2" style={{ color: 'var(--color-text-primary)' }}>
                    Id, Date, DoctorId, DoctorName, Cost
                  </div>
                </div>
                <div>
                  <div className="text-xs font-semibold text-yellow-800 mb-2">AppointmentManagement:</div>
                  <div className="text-xs ml-2 space-y-1" style={{ color: 'var(--color-text-primary)' }}>
                    <div>SlotId (instead of Id)</div>
                    <div>PatientName (Guid type)</div>
                    <div>SlotDate (instead of Date)</div>
                  </div>
                </div>
              </div>
              <div className="mt-4 pt-4 border-t border-yellow-200">
                <span className="text-xs font-medium text-yellow-700">Note: Different property names</span>
              </div>
            </div>
          </div>
        </div>

        {/* Relationships */}
        <div>
          <h3 className="text-lg font-semibold mb-5" style={{ color: 'var(--color-text-primary)' }}>
            Entity Relationships
          </h3>
          <div className="space-y-5">
            <div className="flex items-center space-x-4 p-5 rounded-xl"
                 style={{ backgroundColor: 'var(--color-background-tertiary)' }}>
              <div className="text-center">
                <div className="border-2 rounded-xl p-4"
                     style={{ 
                       backgroundColor: '#EFF6FF',
                       borderColor: '#BFDBFE'
                     }}>
                  <div className="text-sm font-semibold text-blue-900">Slot</div>
                  <div className="text-xs text-blue-700 mt-1">1</div>
                </div>
              </div>
              <ArrowRight className="w-8 h-8" style={{ color: 'var(--color-text-tertiary)' }} />
              <div className="text-center">
                <div className="border-2 rounded-xl p-4"
                     style={{ 
                       backgroundColor: '#D1FAE5',
                       borderColor: '#86EFAC'
                     }}>
                  <div className="text-sm font-semibold text-green-900">AppointmentBooking</div>
                  <div className="text-xs text-green-700 mt-1">0..1</div>
                </div>
              </div>
              <div className="text-sm flex-1" style={{ color: 'var(--color-text-secondary)' }}>
                <p className="font-medium mb-1">Each Slot can have at most one AppointmentBooking</p>
                <p className="text-xs" style={{ color: 'var(--color-text-tertiary)' }}>
                  Connection via SlotId → Slot.Id
                </p>
              </div>
            </div>

            <div className="flex items-center space-x-4 p-5 rounded-xl"
                 style={{ backgroundColor: 'var(--color-background-tertiary)' }}>
              <div className="text-center">
                <div className="border-2 rounded-xl p-4"
                     style={{ 
                       backgroundColor: '#D1FAE5',
                       borderColor: '#86EFAC'
                     }}>
                  <div className="text-sm font-semibold text-green-900">AppointmentBooking</div>
                  <div className="text-xs text-green-700 mt-1">1</div>
                </div>
              </div>
              <ArrowRight className="w-8 h-8" style={{ color: 'var(--color-text-tertiary)' }} />
              <div className="text-center">
                <div className="border-2 rounded-xl p-4"
                     style={{ 
                       backgroundColor: '#EFF6FF',
                       borderColor: '#BFDBFE'
                     }}>
                  <div className="text-sm font-semibold text-blue-900">Slot</div>
                  <div className="text-xs text-blue-700 mt-1">1</div>
                </div>
              </div>
              <div className="text-sm flex-1" style={{ color: 'var(--color-text-secondary)' }}>
                <p className="font-medium mb-1">Each AppointmentBooking references exactly one Slot</p>
                <p className="text-xs" style={{ color: 'var(--color-text-tertiary)' }}>
                  Required relationship via SlotId
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Status Codes */}
        <div>
          <h3 className="text-lg font-semibold mb-5" style={{ color: 'var(--color-text-primary)' }}>
            Appointment Status Codes
          </h3>
          <div className="grid gap-4 md:grid-cols-3">
            <div className="flex items-center space-x-3 p-5 rounded-xl"
                 style={{ backgroundColor: 'var(--color-background-tertiary)' }}>
              <div className="w-12 h-12 rounded-xl flex items-center justify-center text-base font-bold"
                   style={{ backgroundColor: 'var(--color-success)', color: 'white' }}>
                1
              </div>
              <div>
                <div className="font-semibold" style={{ color: 'var(--color-text-primary)' }}>Complete</div>
                <div className="text-xs mt-1" style={{ color: 'var(--color-text-secondary)' }}>
                  Appointment successfully completed
                </div>
              </div>
            </div>
            <div className="flex items-center space-x-3 p-5 rounded-xl"
                 style={{ backgroundColor: 'var(--color-background-tertiary)' }}>
              <div className="w-12 h-12 rounded-xl flex items-center justify-center text-base font-bold"
                   style={{ backgroundColor: 'var(--color-error)', color: 'white' }}>
                2
              </div>
              <div>
                <div className="font-semibold" style={{ color: 'var(--color-text-primary)' }}>Cancelled</div>
                <div className="text-xs mt-1" style={{ color: 'var(--color-text-secondary)' }}>
                  Appointment was cancelled
                </div>
              </div>
            </div>
            <div className="flex items-center space-x-3 p-5 rounded-xl"
                 style={{ backgroundColor: 'var(--color-background-tertiary)' }}>
              <div className="w-12 h-12 rounded-xl flex items-center justify-center text-base font-bold"
                   style={{ backgroundColor: 'var(--color-warning)', color: 'white' }}>
                null
              </div>
              <div>
                <div className="font-semibold" style={{ color: 'var(--color-text-primary)' }}>Pending</div>
                <div className="text-xs mt-1" style={{ color: 'var(--color-text-secondary)' }}>
                  Awaiting completion or cancellation
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Database Information */}
        <div>
          <h3 className="text-lg font-semibold mb-5" style={{ color: 'var(--color-text-primary)' }}>
            Database Configuration
          </h3>
          <div className="space-y-4">
            <div className="grid gap-4 md:grid-cols-3">
              <div className="p-4 border-2 rounded-xl"
                   style={{ borderColor: 'var(--color-border)' }}>
                <h4 className="font-semibold mb-2" style={{ color: 'var(--color-text-primary)' }}>
                  DoctorAvailability
                </h4>
                <code className="text-xs px-2 py-1 rounded block font-mono"
                      style={{ backgroundColor: 'var(--color-background-tertiary)', color: 'var(--color-primary)' }}>
                  doctoravailabilitydb_dev
                </code>
                <p className="text-xs mt-2" style={{ color: 'var(--color-text-secondary)' }}>
                  Port: <strong>5112</strong>
                </p>
              </div>
              <div className="p-4 border-2 rounded-xl"
                   style={{ borderColor: 'var(--color-border)' }}>
                <h4 className="font-semibold mb-2" style={{ color: 'var(--color-text-primary)' }}>
                  AppointmentManagement
                </h4>
                <code className="text-xs px-2 py-1 rounded block font-mono"
                      style={{ backgroundColor: 'var(--color-background-tertiary)', color: 'var(--color-primary)' }}>
                  doctorappointmentmanagementdb_dev
                </code>
                <p className="text-xs mt-2" style={{ color: 'var(--color-text-secondary)' }}>
                  Port: <strong>5113</strong>
                </p>
              </div>
              <div className="p-4 border-2 rounded-xl"
                   style={{ borderColor: 'var(--color-border)' }}>
                <h4 className="font-semibold mb-2" style={{ color: 'var(--color-text-primary)' }}>
                  AppointmentBooking
                </h4>
                <code className="text-xs px-2 py-1 rounded block font-mono"
                      style={{ backgroundColor: 'var(--color-background-tertiary)', color: 'var(--color-primary)' }}>
                  appointmentbookingdb_dev
                </code>
                <p className="text-xs mt-2" style={{ color: 'var(--color-text-secondary)' }}>
                  Port: <strong>5167</strong>
                </p>
              </div>
            </div>
            <div className="p-5 rounded-xl"
                 style={{ 
                   background: 'linear-gradient(135deg, #EFF6FF 0%, #DBEAFE 100%)',
                   border: '2px solid #BFDBFE'
                 }}>
              <h4 className="font-semibold text-base mb-2 text-blue-900">PostgreSQL Connection</h4>
              <code className="text-xs px-3 py-2 rounded-lg block font-mono bg-white"
                    style={{ color: 'var(--color-text-primary)', border: '1px solid #BFDBFE' }}>
                Host=localhost;Port=5432;Database=&lt;dbname&gt;_dev;Username=postgres;Password=123123
              </code>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DataModelVisualization;