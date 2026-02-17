import React, { useEffect, useState } from 'react'
import { Card, CardContent } from '../ui/Card'
import { Button } from '../ui/Button'
import { Input, Textarea, Select } from '../ui/Input'

export const BookingModal = ({ isOpen, doctor, loading = false, onClose, onConfirm }) => {
  const [selectedDate, setSelectedDate] = useState('')
  const [selectedTime, setSelectedTime] = useState('')
  const [durationMinutes, setDurationMinutes] = useState(60)
  const [notes, setNotes] = useState('')

  useEffect(() => {
    if (isOpen) {
      setSelectedDate('')
      setSelectedTime('')
      setDurationMinutes(60)
      setNotes('')
    }
  }, [isOpen])

  if (!isOpen || !doctor) return null

  const minDate = new Date()
  minDate.setDate(minDate.getDate() + 1)
  const maxDate = new Date()
  maxDate.setMonth(maxDate.getMonth() + 3)

  const handleSubmit = (e) => {
    e.preventDefault()

    if (!selectedDate || !selectedTime) {
      alert('Please select both date and time')
      return
    }

    const dateTime = new Date(`${selectedDate}T${selectedTime}:00`)
    if (Number.isNaN(dateTime.getTime())) {
      alert('Invalid date/time')
      return
    }

    onConfirm({
      scheduledDateTime: dateTime.toISOString(),
      durationMinutes,
      notes,
    })
  }

  const doctorName = doctor.user?.firstName && doctor.user?.lastName
    ? `${doctor.user.firstName} ${doctor.user.lastName}`
    : doctor.user?.userName || 'Doctor'

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <Card className="w-full max-w-lg">
        <CardContent className="p-6">
          <div className="flex items-start justify-between mb-4">
            <div>
              <h3 className="text-xl font-semibold text-text-primary">Book Appointment</h3>
              <p className="text-sm text-text-secondary mt-1">Dr. {doctorName} • {doctor.specialty || 'General Consultation'}</p>
            </div>
            <Button variant="ghost" size="sm" onClick={onClose}>✕</Button>
          </div>

          <form onSubmit={handleSubmit}>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Input
                label="Date"
                type="date"
                value={selectedDate}
                onChange={(e) => setSelectedDate(e.target.value)}
                min={minDate.toISOString().split('T')[0]}
                max={maxDate.toISOString().split('T')[0]}
                required
              />
              <Input
                label="Time"
                type="time"
                value={selectedTime}
                onChange={(e) => setSelectedTime(e.target.value)}
                required
              />
            </div>

            <Select
              label="Duration"
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(Number(e.target.value))}
              options={[
                { value: 30, label: '30 minutes' },
                { value: 60, label: '60 minutes' },
                { value: 90, label: '90 minutes' },
                { value: 120, label: '120 minutes' },
              ]}
            />

            <Textarea
              label="Notes (optional)"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Describe symptoms or reason for visit"
              rows={3}
            />

            <div className="flex gap-3 mt-4">
              <Button type="button" variant="outline" className="flex-1" onClick={onClose} disabled={loading}>
                Cancel
              </Button>
              <Button type="submit" variant="primary" className="flex-1" disabled={loading}>
                {loading ? 'Booking...' : 'Confirm Booking'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
