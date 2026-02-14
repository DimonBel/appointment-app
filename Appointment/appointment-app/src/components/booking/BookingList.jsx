import React from 'react'
import { BookingCard, BookingCardSkeleton } from './BookingCard'
import { Icon } from '../ui/Icon'

export const BookingList = ({ appointments, loading, onViewDetails, onReschedule, onCancel }) => {
  if (loading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((item) => (
          <BookingCardSkeleton key={item} />
        ))}
      </div>
    )
  }

  if (appointments.length === 0) {
    return (
      <div className="text-center py-16">
        <Icon name="calendar" size={64} color="#9CA3AF" className="mx-auto mb-4" />
        <p className="text-[16px] font-medium text-gray-900 mb-2">No appointments found</p>
        <p className="text-[14px] text-gray-500">You don't have any bookings in this category</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {appointments.map((appointment) => (
        <BookingCard
          key={appointment.id}
          doctorName={appointment.doctorName}
          specialty={appointment.specialty}
          clinic={appointment.clinic}
          location={appointment.location}
          date={appointment.date}
          time={appointment.time}
          status={appointment.status}
          avatar={appointment.avatar}
          onReschedule={() => onViewDetails?.(appointment)}
          onCancel={() => onCancel?.(appointment)}
        />
      ))}
    </div>
  )
}
