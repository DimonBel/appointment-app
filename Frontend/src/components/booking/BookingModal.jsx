import React, { useEffect, useState } from 'react'
import { useSelector } from 'react-redux'
import { Button } from '../ui/Button'
import { Input, Textarea, Select } from '../ui/Input'
import { appointmentService } from '../../services/appointmentService'
import documentService from '../../services/documentService'
import { Calendar } from './Calendar'
import { TimeSlotPicker } from './TimeSlotPicker'
import { Upload, X, FileText } from 'lucide-react'

export const BookingModal = ({ isOpen, doctor, loading = false, onClose, onConfirm }) => {
  const token = useSelector((state) => state.auth.token)
  const [selectedDate, setSelectedDate] = useState('')
  const [selectedTime, setSelectedTime] = useState('')
  const [durationMinutes, setDurationMinutes] = useState(60)
  const [notes, setNotes] = useState('')
  const [timeSlots, setTimeSlots] = useState([])
  const [slotsLoading, setSlotsLoading] = useState(false)
  const [uploadedFile, setUploadedFile] = useState(null)
  const [uploading, setUploading] = useState(false)

  useEffect(() => {
    if (isOpen) {
      setSelectedDate('')
      setSelectedTime('')
      setDurationMinutes(60)
      setNotes('')
      setTimeSlots([])
      setUploadedFile(null)
    }
  }, [isOpen])

  useEffect(() => {
    const loadSlots = async () => {
      if (!isOpen || !selectedDate || !doctor?.professionalId || !token) {
        setTimeSlots([])
        return
      }

      try {
        setSlotsLoading(true)
        const slots = await appointmentService.getAvailabilitySlots(doctor.professionalId, selectedDate, token)
        const normalizedSlots = Array.isArray(slots)
          ? slots
              .map((slot) => {
                const startTimeRaw = slot.startTime
                const endTimeRaw = slot.endTime
                let normalizedTime = ''
                let normalizedEndTime = ''

                if (typeof startTimeRaw === 'string') {
                  normalizedTime = startTimeRaw.slice(0, 5)
                } else if (startTimeRaw && typeof startTimeRaw === 'object') {
                  normalizedTime = `${String(startTimeRaw.hours ?? 0).padStart(2, '0')}:${String(startTimeRaw.minutes ?? 0).padStart(2, '0')}`
                }

                if (typeof endTimeRaw === 'string') {
                  normalizedEndTime = endTimeRaw.slice(0, 5)
                } else if (endTimeRaw && typeof endTimeRaw === 'object') {
                  normalizedEndTime = `${String(endTimeRaw.hours ?? 0).padStart(2, '0')}:${String(endTimeRaw.minutes ?? 0).padStart(2, '0')}`
                }

                return {
                  id: slot.id,
                  time: normalizedTime,
                  endTime: normalizedEndTime,
                  isAvailable: Boolean(slot.isAvailable),
                }
              })
              .filter((slot) => slot.time)
          : []

        setTimeSlots(normalizedSlots)

        if (selectedTime) {
          const selectedSlot = normalizedSlots.find((slot) => slot.time === selectedTime)
          if (!selectedSlot || !selectedSlot.isAvailable) {
            setSelectedTime('')
          }
        }
      } catch (error) {
        console.error('Error loading slots:', error)
        setTimeSlots([])
      } finally {
        setSlotsLoading(false)
      }
    }

    loadSlots()
  }, [isOpen, selectedDate, doctor?.professionalId, token])

  if (!isOpen || !doctor) return null

  const toMinutes = (time) => {
    const [hour, minute] = time.split(':').map(Number)
    return hour * 60 + minute
  }

  const getMaxContinuousDuration = (startTime) => {
    if (!startTime) return 0

    const availableSlots = [...timeSlots]
      .filter((slot) => slot.isAvailable && slot.time && slot.endTime)
      .sort((left, right) => toMinutes(left.time) - toMinutes(right.time))

    const startSlot = availableSlots.find((slot) => slot.time === startTime)
    if (!startSlot) return 0

    let currentEnd = startSlot.endTime

    while (true) {
      const nextSlot = availableSlots.find((slot) => slot.time === currentEnd)
      if (!nextSlot) break
      currentEnd = nextSlot.endTime
    }

    return toMinutes(currentEnd) - toMinutes(startTime)
  }

  const minDate = new Date()
  minDate.setDate(minDate.getDate() + 1)
  const maxDate = new Date()
  maxDate.setMonth(maxDate.getMonth() + 3)

  const handleFileUpload = async (e) => {
    const file = e.target.files[0]
    if (!file) return

    // Validate file type
    const allowedTypes = [
      'application/pdf',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'application/vnd.ms-excel',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      'text/plain'
    ]
    if (!allowedTypes.includes(file.type)) {
      alert('Please upload a PDF, DOCX, XLSX, or TXT file')
      return
    }

    // Validate file size (100MB max)
    if (file.size > 100 * 1024 * 1024) {
      alert('File size must be less than 100MB')
      return
    }

    setUploading(true)
    try {
      const uploadedDoc = await documentService.uploadDocument(
        file,
        {
          documentType: 'BookingFile',
          linkedEntityType: 'Order',
          linkedEntityId: null // Will be set after booking is created
        },
        token
      )
      console.log('Setting uploadedFile:', uploadedDoc)
      setUploadedFile(uploadedDoc)
    } catch (error) {
      console.error('Error uploading file:', error)
      alert('Failed to upload file. Please try again.')
    } finally {
      setUploading(false)
    }
  }

  const handleRemoveFile = () => {
    setUploadedFile(null)
  }

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

    const maxDuration = getMaxContinuousDuration(selectedTime)
    if (maxDuration > 0 && durationMinutes > maxDuration) {
      alert(`Selected time supports up to ${maxDuration} minutes. Please choose a shorter duration.`)
      return
    }

    onConfirm({
      scheduledDateTime: `${selectedDate}T${selectedTime}:00`,
      durationMinutes,
      notes,
      uploadedFile,
    })
  }

  const doctorName = doctor.user?.firstName && doctor.user?.lastName
    ? `${doctor.user.firstName} ${doctor.user.lastName}`
    : doctor.user?.userName || 'Doctor'

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center z-50 p-4 md:p-8">
      <div className="w-full max-w-5xl max-h-[85vh] overflow-y-auto">
        <div className="bg-gray-50 rounded-3xl shadow-2xl overflow-hidden">
          <form onSubmit={handleSubmit}>
            <div className="bg-white border-b border-gray-100 px-5 py-3 md:px-6 md:py-4">
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-2xl font-bold text-gray-900">Book Appointment</h2>
                  <p className="text-gray-500 mt-1">Dr. {doctorName} • {doctor.specialty || 'General Consultation'}</p>
                </div>
                <button
                  type="button"
                  onClick={onClose}
                  className="p-2 hover:bg-gray-100 rounded-full transition-colors"
                >
                  <svg className="w-6 h-6 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>

            <div className="p-5 md:p-6">
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
                <Calendar
                  selectedDate={selectedDate}
                  onSelectDate={setSelectedDate}
                  minDate={minDate}
                  maxDate={maxDate}
                />
                
                {selectedDate ? (
                  <TimeSlotPicker
                    slots={timeSlots}
                    selectedTime={selectedTime}
                    onSelectTime={setSelectedTime}
                    loading={slotsLoading}
                  />
                ) : (
                  <div className="bg-white rounded-3xl shadow-medium p-5 flex items-center justify-center">
                    <div className="text-center">
                      <svg className="w-14 h-14 text-gray-300 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                      </svg>
                      <p className="text-gray-500 text-sm">Select a date to see available time slots</p>
                    </div>
                  </div>
                )}
              </div>

              <div className="bg-white rounded-3xl shadow-medium p-5 mb-4">
                <h3 className="text-lg font-semibold text-gray-900 mb-3">Appointment Details</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Duration</label>
                    <select
                      value={durationMinutes}
                      onChange={(e) => setDurationMinutes(Number(e.target.value))}
                      className="w-full px-4 py-2.5 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-accent focus:border-transparent transition-all"
                    >
                      <option value={30}>30 minutes</option>
                      <option value={60}>60 minutes</option>
                      <option value={90}>90 minutes</option>
                      <option value={120}>120 minutes</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Notes (optional)</label>
                    <textarea
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                      placeholder="Describe symptoms or reason for visit"
                      rows={1}
                      className="w-full px-4 py-2.5 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-accent focus:border-transparent transition-all resize-none"
                    />
                  </div>
                </div>
                
                {/* File Upload */}
                <div className="mt-3">
                  <label className="block text-sm font-medium text-gray-700 mb-2">Attach File (optional)</label>
                  {uploadedFile ? (
                    <div className="flex items-center gap-3 p-2.5 bg-primary-accent/10 border border-primary-accent/20 rounded-xl">
                      <FileText size={20} className="text-primary-accent" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-900 truncate">
                          {uploadedFile.originalFileName}
                        </p>
                        <p className="text-xs text-gray-500">
                          {documentService.formatFileSize(uploadedFile.fileSize)}
                        </p>
                      </div>
                      <button
                        type="button"
                        onClick={handleRemoveFile}
                        className="p-1.5 hover:bg-white rounded-lg transition-colors"
                        disabled={loading}
                      >
                        <X size={16} className="text-gray-500" />
                      </button>
                    </div>
                  ) : (
                    <div className="relative">
                      <input
                        type="file"
                        id="booking-file-upload"
                        accept=".pdf,.doc,.docx,.xls,.xlsx,.txt"
                        onChange={handleFileUpload}
                        disabled={uploading || loading}
                        className="absolute inset-0 w-full h-full opacity-0 cursor-pointer"
                      />
                      <div
                        className={`flex items-center gap-3 px-4 py-2.5 border-2 border-dashed rounded-xl transition-all ${
                          uploading || loading
                            ? 'border-gray-300 bg-gray-50 cursor-not-allowed'
                            : 'border-gray-300 hover:border-primary-accent hover:bg-primary-accent/5 cursor-pointer'
                        }`}
                      >
                        <Upload
                          size={20}
                          className={uploading ? 'text-gray-400 animate-pulse' : 'text-gray-400'}
                        />
                        <span className="text-sm text-gray-600">
                          {uploading ? 'Uploading...' : 'Click to upload PDF, DOCX, XLSX, or TXT'}
                        </span>
                      </div>
                    </div>
                  )}
                  <p className="text-xs text-gray-500 mt-1">Max file size: 100MB</p>
                </div>
              </div>

              <div className="flex justify-end gap-3">
                <Button
                  type="button"
                  variant="outline"
                  onClick={onClose}
                  disabled={loading}
                  className="px-6 py-2.5 rounded-xl"
                >
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  disabled={loading || !selectedDate || !selectedTime}
                  className="px-6 py-2.5 rounded-xl"
                >
                  {loading ? 'Booking...' : 'Confirm Booking'}
                </Button>
              </div>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
