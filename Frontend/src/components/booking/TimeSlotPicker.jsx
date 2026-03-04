import React from 'react'

export const TimeSlotPicker = ({ slots, selectedTime, onSelectTime, loading, selectedDate }) => {
  const formatTimeLabel = (time) => {
    const [hourStr, minuteStr] = time.split(':')
    const hour = Number(hourStr)
    const minute = Number(minuteStr)
    const period = hour >= 12 ? 'PM' : 'AM'
    const displayHour = hour % 12 || 12
    return `${displayHour}:${String(minute).padStart(2, '0')} ${period}`
  }

  const isSlotInPast = (time, date) => {
    if (!date) return false
    
    const now = new Date()
    const slotDate = new Date(date)
    
    // Check if the slot date is today
    const isToday = slotDate.toDateString() === now.toDateString()
    
    if (!isToday) return false
    
    // Parse the slot time
    const [hourStr, minuteStr] = time.split(':')
    const hour = Number(hourStr)
    const minute = Number(minuteStr)
    
    // Set the slot datetime
    slotDate.setHours(hour, minute, 0, 0)
    
    // Check if slot time is in the past
    return slotDate < now
  }

  // Filter slots to show only 60-minute intervals (hourly slots)
  const hourlySlots = slots.filter(slot => {
    const [hourStr, minuteStr] = slot.time.split(':')
    const minute = Number(minuteStr)
    // Only show slots that start at :00 (hourly)
    return minute === 0
  })

  if (loading) {
    return (
      <div className="bg-white rounded-3xl shadow-medium p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Select Hour</h3>
        <div className="flex items-center justify-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-2 border-primary-dark border-t-transparent"></div>
        </div>
      </div>
    )
  }

  if (!hourlySlots || hourlySlots.length === 0) {
    return (
      <div className="bg-white rounded-3xl shadow-medium p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Select Hour</h3>
        <div className="flex items-center justify-center py-8">
          <p className="text-gray-500 text-sm">No time slots available for this date</p>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-white rounded-3xl shadow-medium p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">Select Hour</h3>
      <div className="grid grid-cols-3 gap-3">
        {hourlySlots.map((slot) => {
          const isSelected = selectedTime === slot.time
          const isNotAvailable = !slot.isAvailable
          const isPastSlot = isSlotInPast(slot.time, selectedDate)
          const isDisabled = isNotAvailable || isPastSlot

          return (
            <button
              key={slot.id}
              type="button"
              disabled={isDisabled}
              onClick={() => onSelectTime(slot.time)}
              className={`
                h-12 rounded-full text-sm font-medium transition-all duration-200
                ${isDisabled
                  ? 'bg-gray-100 text-gray-300 cursor-not-allowed'
                  : isSelected
                    ? 'bg-primary-dark text-white shadow-md scale-105'
                    : 'bg-gray-50 text-gray-700 hover:bg-gray-100 hover:scale-105 border border-gray-200'
                }
              `}
              title={isPastSlot ? 'Past time' : isNotAvailable ? 'Unavailable' : 'Available'}
            >
              {formatTimeLabel(slot.time)}
            </button>
          )
        })}
      </div>
    </div>
  )
}