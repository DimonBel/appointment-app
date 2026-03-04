import React, { useState, useMemo } from 'react'

export const Calendar = ({ 
  selectedDate, 
  onSelectDate, 
  minDate, 
  maxDate, 
  availableDates = [],
  availabilityStatus = {},
  loadingAvailability = false,
  currentMonth: controlledMonth,
  onMonthChange
}) => {
  const [internalMonth, setInternalMonth] = useState(new Date())
  const currentMonth = controlledMonth || internalMonth

  const daysOfWeek = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

  const getDaysInMonth = (date) => {
    const year = date.getFullYear()
    const month = date.getMonth()
    const firstDay = new Date(year, month, 1)
    const lastDay = new Date(year, month + 1, 0)
    const daysInMonth = lastDay.getDate()
    const startingDayOfWeek = firstDay.getDay()

    const days = []

    for (let i = 0; i < startingDayOfWeek; i++) {
      days.push(null)
    }

    for (let day = 1; day <= daysInMonth; day++) {
      days.push(new Date(year, month, day))
    }

    return days
  }

  const formatDateKey = (date) => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
  }

  const isDateDisabled = (date) => {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    
    const dateOnly = new Date(date)
    dateOnly.setHours(0, 0, 0, 0)

    if (minDate) {
      const minDateOnly = new Date(minDate)
      minDateOnly.setHours(0, 0, 0, 0)
      if (dateOnly < minDateOnly) return true
    }

    if (maxDate) {
      const maxDateOnly = new Date(maxDate)
      maxDateOnly.setHours(0, 0, 0, 0)
      if (dateOnly > maxDateOnly) return true
    }

    // Only disable dates BEFORE today, not today itself
    if (dateOnly < today) return true

    // Check if date has available slots from availability status
    const dateKey = formatDateKey(date)
    if (availabilityStatus[dateKey]) {
      return !availabilityStatus[dateKey].hasAvailableSlots
    }

    if (availableDates.length > 0) {
      return !availableDates.includes(formatDateKey(date))
    }

    return false
  }

  const isPastDate = (date) => {
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    const dateOnly = new Date(date)
    dateOnly.setHours(0, 0, 0, 0)
    return dateOnly < today
  }

  const isSelected = (date) => {
    if (!selectedDate) return false
    return formatDateKey(date) === selectedDate
  }

  const handleDateClick = (date) => {
    if (isDateDisabled(date)) return
    onSelectDate(formatDateKey(date))
  }

  const goToPreviousMonth = () => {
    const newMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1)
    if (onMonthChange) {
      onMonthChange(newMonth)
    } else {
      setInternalMonth(newMonth)
    }
  }

  const goToNextMonth = () => {
    const newMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1)
    if (onMonthChange) {
      onMonthChange(newMonth)
    } else {
      setInternalMonth(newMonth)
    }
  }

  const days = getDaysInMonth(currentMonth)

  const monthYear = currentMonth.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })

  return (
    <div className="bg-white rounded-3xl shadow-medium p-6">
      <div className="flex items-center justify-between mb-6">
        <button
          type="button"
          onClick={goToPreviousMonth}
          className="p-2 hover:bg-gray-100 rounded-full transition-colors"
        >
          <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </button>
        <h3 className="text-lg font-semibold text-gray-900">{monthYear}</h3>
        <button
          type="button"
          onClick={goToNextMonth}
          className="p-2 hover:bg-gray-100 rounded-full transition-colors"
        >
          <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </button>
      </div>

      <div className="grid grid-cols-7 gap-2 mb-2">
        {daysOfWeek.map((day) => (
          <div key={day} className="text-center text-xs font-medium text-gray-500 py-2">
            {day}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7 gap-2">
        {days.map((date, index) => {
          if (!date) {
            return <div key={`empty-${index}`} className="aspect-square" />
          }

          const disabled = isDateDisabled(date)
          const selected = isSelected(date)
          const past = isPastDate(date)
          const dateKey = formatDateKey(date)
          const status = availabilityStatus[dateKey]

          // Determine availability indicator - ONLY show for non-past dates
          let showIndicator = false
          let indicatorColor = 'transparent'
          
          if (status && !past) {
            if (status.hasAvailableSlots) {
              showIndicator = true
              indicatorColor = 'bg-green-500'
            } else if (status.hasSlots && !status.hasAvailableSlots) {
              showIndicator = true
              indicatorColor = 'bg-gray-400'
            }
          }

          return (
            <button
              key={index}
              type="button"
              onClick={() => handleDateClick(date)}
              disabled={disabled}
              className={`
                aspect-square rounded-2xl flex flex-col items-center justify-center text-sm font-medium transition-all duration-200 relative
                ${selected
                  ? 'bg-primary-dark text-white shadow-lg scale-105'
                  : disabled
                    ? past
                      ? 'text-gray-400 cursor-not-allowed bg-gray-100'
                      : 'text-gray-300 cursor-not-allowed bg-gray-50'
                    : 'text-gray-700 hover:bg-gray-100 hover:scale-105 bg-white'
                }
              `}
            >
              <span className="relative z-10">{date.getDate()}</span>
              {showIndicator && !selected && (
                <div className={`absolute top-2 right-2 w-2 h-2 rounded-full ${indicatorColor} shadow-sm`} />
              )}
            </button>
          )
        })}
      </div>

      {/* Legend */}
      <div className="flex items-center justify-center gap-6 mt-4 pt-4 border-t border-gray-100">
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full bg-green-500 shadow-sm" />
          <span className="text-xs text-gray-600">Available</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full bg-gray-400 shadow-sm" />
          <span className="text-xs text-gray-600">Fully Booked</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 rounded-full bg-gray-50 border border-gray-200" />
          <span className="text-xs text-gray-600">Unavailable</span>
        </div>
      </div>
    </div>
  )
}