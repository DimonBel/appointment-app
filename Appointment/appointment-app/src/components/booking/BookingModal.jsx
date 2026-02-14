import React, { useState } from 'react'
import { Button } from '../ui/Button'
import { Icon } from '../ui/Icon'

export const BookingModal = ({ isOpen, onClose, professional, onConfirm }) => {
  const [selectedDate, setSelectedDate] = useState('')
  const [selectedTime, setSelectedTime] = useState('')
  const [duration, setDuration] = useState(60)
  const [notes, setNotes] = useState('')

  if (!isOpen) return null

  // Generate time slots (9 AM to 5 PM)
  const generateTimeSlots = () => {
    const slots = []
    for (let hour = 9; hour <= 17; hour++) {
      slots.push(`${hour.toString().padStart(2, '0')}:00`)
      if (hour < 17) {
        slots.push(`${hour.toString().padStart(2, '0')}:30`)
      }
    }
    return slots
  }

  // Get minimum date (tomorrow)
  const getMinDate = () => {
    const tomorrow = new Date()
    tomorrow.setDate(tomorrow.getDate() + 1)
    return tomorrow.toISOString().split('T')[0]
  }

  // Get maximum date (3 months from now)
  const getMaxDate = () => {
    const maxDate = new Date()
    maxDate.setMonth(maxDate.getMonth() + 3)
    return maxDate.toISOString().split('T')[0]
  }

  const handleSubmit = (e) => {
    e.preventDefault()
    
    if (!selectedDate || !selectedTime) {
      alert('Please select both date and time')
      return
    }

    const dateTime = new Date(`${selectedDate}T${selectedTime}:00`)
    
    onConfirm({
      professionalId: professional.id,
      scheduledDateTime: dateTime.toISOString(),
      durationMinutes: duration,
      title: `Appointment with ${professional.title || ''} ${professional.user?.firstName || ''} ${professional.user?.lastName || ''}`.trim(),
      description: notes || `Consultation for ${professional.specialization || 'general services'}`,
      domainConfigurationId: null
    })
  }

  const timeSlots = generateTimeSlots()

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl max-w-md w-full max-h-[90vh] overflow-y-auto">
        <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
          <h2 className="text-xl font-semibold text-[#1E2A38]">Book Appointment</h2>
          <button 
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <Icon name="x" className="w-6 h-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6">
          {/* Professional Info */}
          <div className="flex items-center gap-4 mb-6 p-4 bg-gray-50 rounded-lg">
            <div className="w-12 h-12 rounded-full bg-gray-200 overflow-hidden flex-shrink-0">
              <img 
                src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${professional.id}`}
                alt={professional.user?.firstName || 'Professional'}
                className="w-full h-full object-cover"
              />
            </div>
            <div>
              <h3 className="font-semibold text-[#1E2A38]">
                {professional.title || ''} {professional.user?.firstName || ''} {professional.user?.lastName || ''}
              </h3>
              <p className="text-sm text-gray-600">{professional.specialization}</p>
            </div>
          </div>

          {/* Date Selection */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Date
            </label>
            <input
              type="date"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              min={getMinDate()}
              max={getMaxDate()}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#1E2A38] focus:border-transparent"
              required
            />
          </div>

          {/* Time Selection */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Select Time
            </label>
            <select
              value={selectedTime}
              onChange={(e) => setSelectedTime(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#1E2A38] focus:border-transparent"
              required
            >
              <option value="">Choose a time slot</option>
              {timeSlots.map((time) => (
                <option key={time} value={time}>
                  {time}
                </option>
              ))}
            </select>
          </div>

          {/* Duration Selection */}
          <div className="mb-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Duration
            </label>
            <select
              value={duration}
              onChange={(e) => setDuration(Number(e.target.value))}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#1E2A38] focus:border-transparent"
            >
              <option value={30}>30 minutes</option>
              <option value={60}>1 hour</option>
              <option value={90}>1.5 hours</option>
              <option value={120}>2 hours</option>
            </select>
          </div>

          {/* Notes */}
          <div className="mb-6">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Notes (Optional)
            </label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Add any special requirements or reasons for the visit..."
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-[#1E2A38] focus:border-transparent resize-none"
              rows={3}
            />
          </div>

          {/* Appointment Summary */}
          {selectedDate && selectedTime && (
            <div className="mb-6 p-4 bg-blue-50 rounded-lg border border-blue-200">
              <h4 className="font-medium text-[#1E2A38] mb-2">Appointment Summary</h4>
              <div className="text-sm text-gray-600 space-y-1">
                <p>📅 {new Date(selectedDate).toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</p>
                <p>🕐 {selectedTime}</p>
                <p>⏱️ {duration} minutes</p>
              </div>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex gap-3">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              className="flex-1 bg-[#1E2A38] text-white hover:bg-[#2A3A4A]"
            >
              Confirm Booking
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
