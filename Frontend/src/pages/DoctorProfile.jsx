import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Card, CardContent } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Loader } from '../components/ui/Loader'
import { doctorProfileService } from '../services/doctorProfileService'
import { appointmentService } from '../services/appointmentService'
import { Save, Trash2, Plus, X } from 'lucide-react'

export const DoctorProfile = () => {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [formData, setFormData] = useState({
    specialty: '',
    bio: '',
    qualifications: '',
    yearsOfExperience: 0,
    services: [],
    consultationFee: '',
    languages: [],
    address: '',
    city: '',
    country: '',
    isAvailableForAppointments: true,
  })
  const [newService, setNewService] = useState('')
  const [newLanguage, setNewLanguage] = useState('')
  const [professionalEntity, setProfessionalEntity] = useState(null)
  const [availabilityItems, setAvailabilityItems] = useState([])
  const [availabilityLoading, setAvailabilityLoading] = useState(false)
  const [addingAvailability, setAddingAvailability] = useState(false)
  const [availabilityForm, setAvailabilityForm] = useState({
    dayOfWeek: '1',
    startTime: '09:00',
    endTime: '17:00',
  })

  const token = useSelector((state) => state.auth.token)
  const user = useSelector((state) => state.auth.user)

  useEffect(() => {
    if (token) {
      fetchProfile()
      fetchProfessionalAvailability()
    } else {
      setLoading(false)
      setError('Please login to manage your professional profile')
    }
  }, [token, user?.id])

  const fetchProfile = async () => {
    setLoading(true)
    setError('')
    try {
      const data = await doctorProfileService.getMyProfile(token)

      if (!data) {
        setProfile(null)
        setFormData({
          specialty: '',
          bio: '',
          qualifications: '',
          yearsOfExperience: 0,
          services: [],
          consultationFee: '',
          languages: [],
          address: '',
          city: '',
          country: '',
          isAvailableForAppointments: true,
        })
        return
      }

      setProfile(data)
      setFormData({
        specialty: data.specialty || '',
        bio: data.bio || '',
        qualifications: data.qualifications || '',
        yearsOfExperience: data.yearsOfExperience || 0,
        services: data.services || [],
        consultationFee: data.consultationFee?.toString() || '',
        languages: data.languages || [],
        address: data.address || '',
        city: data.city || '',
        country: data.country || '',
        isAvailableForAppointments: data.isAvailableForAppointments ?? true,
      })
    } catch (err) {
      if (err.response?.status === 401) {
        setError('Unauthorized. Please login again.')
      } else if (err.response?.status === 404 || err.response?.status === 500) {
        setError('User profile not found. Please logout and login with a valid account.')
      } else {
        setError(err.response?.data?.message || 'Failed to load profile')
      }
    } finally {
      setLoading(false)
    }
  }

  const normalizeTimeToApi = (timeValue) => `${timeValue}:00`

  const ensureProfessionalExists = async () => {
    if (!token || !user?.id) {
      throw new Error('User not authenticated')
    }

    let professional = await appointmentService.getProfessionalByUserId(user.id, token)

    if (!professional) {
      try {
        professional = await appointmentService.createProfessional({
          userId: user.id,
          title: 'Dr.',
          qualifications: formData.qualifications || null,
          specialization: formData.specialty || 'General',
        }, token)
      } catch (err) {
        if (err.response?.status === 500 || err.response?.status === 404) {
          throw new Error('Unable to create professional profile. Your user account may have been removed. Please logout and login with a valid account.')
        }
        throw err
      }
    }

    setProfessionalEntity(professional)
    return professional
  }

  const fetchProfessionalAvailability = async () => {
    if (!token || !user?.id) {
      setProfessionalEntity(null)
      setAvailabilityItems([])
      return
    }

    setAvailabilityLoading(true)
    try {
      const professional = await ensureProfessionalExists()

      const availabilities = await appointmentService.getAvailabilitiesByProfessional(professional.id, token)
      const items = Array.isArray(availabilities) ? availabilities : []
      setAvailabilityItems(items)
    } catch (err) {
      console.error('Failed to load professional availability', err)
      if (err.message && err.message.includes('logout')) {
        setError(err.message)
      }
      setAvailabilityItems([])
    } finally {
      setAvailabilityLoading(false)
    }
  }

  const handleAvailabilityFieldChange = (e) => {
    const { name, value } = e.target
    setAvailabilityForm((prev) => ({
      ...prev,
      [name]: value,
    }))
  }

  const handleAddAvailability = async () => {
    setError('')
    setSuccess('')

    if (!availabilityForm.startTime || !availabilityForm.endTime) {
      setError('Please select both start and end time for slot setup.')
      return
    }

    if (availabilityForm.startTime >= availabilityForm.endTime) {
      setError('Start time must be earlier than end time.')
      return
    }

    setAddingAvailability(true)

    try {
      let professional = professionalEntity

      // Ensure professional entity exists
      if (!professional?.id) {
        professional = await ensureProfessionalExists()
      }

      await appointmentService.createAvailability({
        professionalId: professional.id,
        dayOfWeek: Number(availabilityForm.dayOfWeek),
        startTime: normalizeTimeToApi(availabilityForm.startTime),
        endTime: normalizeTimeToApi(availabilityForm.endTime),
        scheduleType: 1,
      }, token)

      setSuccess('Time slot schedule added successfully!')
      await fetchProfessionalAvailability()
    } catch (err) {
      console.error('Error adding availability:', err)
      const errorMessage = err.response?.data?.message || err.response?.data?.error || 'Failed to add time slot schedule'
      setError(errorMessage)
    } finally {
      setAddingAvailability(false)
    }
  }

  const handleDeleteAvailability = async (availabilityId) => {
    setError('')
    setSuccess('')

    try {
      await appointmentService.deleteAvailability(availabilityId, token)
      setSuccess('Time slot schedule deleted successfully!')
      await fetchProfessionalAvailability()
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to delete time slot schedule')
    }
  }

  const dayLabels = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? checked : value,
    })
  }

  const handleAddService = () => {
    if (newService.trim()) {
      setFormData({
        ...formData,
        services: [...formData.services, newService.trim()],
      })
      setNewService('')
    }
  }

  const handleRemoveService = (index) => {
    setFormData({
      ...formData,
      services: formData.services.filter((_, i) => i !== index),
    })
  }

  const handleAddLanguage = () => {
    if (newLanguage.trim()) {
      setFormData({
        ...formData,
        languages: [...formData.languages, newLanguage.trim()],
      })
      setNewLanguage('')
    }
  }

  const handleRemoveLanguage = (index) => {
    setFormData({
      ...formData,
      languages: formData.languages.filter((_, i) => i !== index),
    })
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setSuccess('')
    setSaving(true)

    try {
      // Normalize consultationFee to handle both comma and dot as decimal separator
      const normalizedFee = formData.consultationFee
        ? formData.consultationFee.toString().replace(',', '.')
        : null

      const profileData = {
        ...formData,
        userId: user?.id, // Include userId - required by backend API
        yearsOfExperience: parseInt(formData.yearsOfExperience) || 0,
        consultationFee: normalizedFee ? parseFloat(normalizedFee) : null,
      }

      if (profile) {
        await doctorProfileService.updateProfile(profileData, token)
        setSuccess('Profile updated successfully!')
      } else {
        await doctorProfileService.createProfile(profileData, token)
        setSuccess('Profile created successfully!')
      }

      await fetchProfile()
      await fetchProfessionalAvailability()
    } catch (err) {
      console.error('Error saving profile:', err)
      setError(err.response?.data?.message || err.message || 'Failed to save profile')
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <MainContent>
        <div className="flex justify-center py-12">
          <Loader size="lg" />
        </div>
      </MainContent>
    )
  }

  return (
    <MainContent>
      <SectionHeader
        title="My Professional Profile"
        subtitle="Manage your professional information and services"
      />

      {error && (
        <div className="mb-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-600">
          {error}
        </div>
      )}

      {success && (
        <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg text-green-600">
          {success}
        </div>
      )}

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit}>
            {/* Basic Information */}
            <div className="mb-6">
              <h3 className="text-lg font-semibold text-text-primary mb-4">Basic Information</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Input
                  label="Specialty"
                  name="specialty"
                  value={formData.specialty}
                  onChange={handleChange}
                  placeholder="e.g., Cardiology, Dermatology"
                  required
                />
                <Input
                  label="Years of Experience"
                  type="number"
                  name="yearsOfExperience"
                  value={formData.yearsOfExperience}
                  onChange={handleChange}
                  min="0"
                  required
                />
              </div>
            </div>

            {/* Bio */}
            <div className="mb-6">
              <label className="block text-sm font-medium text-text-primary mb-2">
                Bio
              </label>
              <textarea
                name="bio"
                value={formData.bio}
                onChange={handleChange}
                rows="4"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
                placeholder="Tell patients about yourself and your practice..."
                required
              />
            </div>

            {/* Qualifications */}
            <div className="mb-6">
              <label className="block text-sm font-medium text-text-primary mb-2">
                Qualifications
              </label>
              <textarea
                name="qualifications"
                value={formData.qualifications}
                onChange={handleChange}
                rows="3"
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
                placeholder="Your degrees, certifications, and professional qualifications..."
              />
            </div>

            {/* Services */}
            <div className="mb-6">
              <h3 className="text-lg font-semibold text-text-primary mb-4">Services Offered</h3>
              <div className="flex gap-2 mb-3">
                <input
                  type="text"
                  value={newService}
                  onChange={(e) => setNewService(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddService())}
                  placeholder="Add a service..."
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
                />
                <Button type="button" onClick={handleAddService} variant="secondary">
                  <Plus size={20} />
                </Button>
              </div>
              <div className="flex flex-wrap gap-2">
                {formData.services.map((service, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center gap-2 px-3 py-1 bg-primary-accent/10 text-primary-accent rounded-full text-sm"
                  >
                    {service}
                    <button
                      type="button"
                      onClick={() => handleRemoveService(index)}
                      className="hover:bg-primary-accent/20 rounded-full p-1"
                    >
                      <X size={14} />
                    </button>
                  </span>
                ))}
              </div>
            </div>

            {/* Languages */}
            <div className="mb-6">
              <h3 className="text-lg font-semibold text-text-primary mb-4">Languages Spoken</h3>
              <div className="flex gap-2 mb-3">
                <input
                  type="text"
                  value={newLanguage}
                  onChange={(e) => setNewLanguage(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), handleAddLanguage())}
                  placeholder="Add a language..."
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
                />
                <Button type="button" onClick={handleAddLanguage} variant="secondary">
                  <Plus size={20} />
                </Button>
              </div>
              <div className="flex flex-wrap gap-2">
                {formData.languages.map((language, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center gap-2 px-3 py-1 bg-primary-accent/10 text-primary-accent rounded-full text-sm"
                  >
                    {language}
                    <button
                      type="button"
                      onClick={() => handleRemoveLanguage(index)}
                      className="hover:bg-primary-accent/20 rounded-full p-1"
                    >
                      <X size={14} />
                    </button>
                  </span>
                ))}
              </div>
            </div>

            {/* Practice Details */}
            <div className="mb-6">
              <h3 className="text-lg font-semibold text-text-primary mb-4">Practice Details</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Input
                  label="Consultation Fee"
                  type="number"
                  name="consultationFee"
                  value={formData.consultationFee}
                  onChange={handleChange}
                  placeholder="0.00"
                  step="0.01"
                  min="0"
                />
              </div>
            </div>

            {/* Location */}
            <div className="mb-6">
              <h3 className="text-lg font-semibold text-text-primary mb-4">Location</h3>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
                <Input
                  label="City"
                  name="city"
                  value={formData.city}
                  onChange={handleChange}
                  placeholder="City"
                />
                <Input
                  label="Country"
                  name="country"
                  value={formData.country}
                  onChange={handleChange}
                  placeholder="Country"
                />
              </div>
              <Input
                label="Address"
                name="address"
                value={formData.address}
                onChange={handleChange}
                placeholder="Full address"
              />
            </div>

            {/* Availability */}
            <div className="mb-6">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  name="isAvailableForAppointments"
                  checked={formData.isAvailableForAppointments}
                  onChange={handleChange}
                  className="w-4 h-4 text-primary-accent border-gray-300 rounded focus:ring-primary-accent"
                />
                <span className="text-sm text-text-primary">Available for appointments</span>
              </label>
            </div>

            <div className="mb-8">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">Weekly Schedule</h3>
                  <p className="text-sm text-gray-500 mt-1">
                    Configure your weekly slot schedule. Patients can book appointments during these times.
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <div className="text-xs text-gray-400 bg-gray-100 px-3 py-1.5 rounded-full">
                    Timezone: Local
                  </div>
                  {professionalEntity?.id && (
                    <div className="text-xs text-green-600 bg-green-50 px-3 py-1.5 rounded-full flex items-center gap-1">
                      <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                        <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                      </svg>
                      Professional Ready
                    </div>
                  )}
                </div>
              </div>

              <div className="bg-white rounded-2xl shadow-medium p-6 mb-4">
                <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Day of Week</label>
                    <select
                      name="dayOfWeek"
                      value={availabilityForm.dayOfWeek}
                      onChange={handleAvailabilityFieldChange}
                      className="w-full px-4 py-3 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-accent focus:border-transparent transition-all bg-gray-50 hover:bg-white"
                    >
                      {dayLabels.map((day, index) => (
                        <option key={day} value={String(index)}>{day}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">Start Time</label>
                    <input
                      type="time"
                      name="startTime"
                      value={availabilityForm.startTime}
                      onChange={handleAvailabilityFieldChange}
                      className="w-full px-4 py-3 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-accent focus:border-transparent transition-all bg-gray-50 hover:bg-white"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">End Time</label>
                    <input
                      type="time"
                      name="endTime"
                      value={availabilityForm.endTime}
                      onChange={handleAvailabilityFieldChange}
                      className="w-full px-4 py-3 border border-gray-200 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-accent focus:border-transparent transition-all bg-gray-50 hover:bg-white"
                    />
                  </div>

                  <div className="flex items-end">
                    <Button
                      type="button"
                      onClick={handleAddAvailability}
                      disabled={addingAvailability || availabilityLoading}
                      className="w-full h-11 rounded-xl bg-primary-dark hover:bg-primary-dark/90 text-white font-medium transition-all"
                    >
                      <Plus size={18} className="mr-2" />
                      {addingAvailability ? 'Adding...' : 'Add Schedule'}
                    </Button>
                  </div>
                </div>

                <div className="flex items-start gap-2 text-xs text-gray-500 bg-blue-50 p-3 rounded-xl">
                  <svg className="w-4 h-4 text-blue-500 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                  </svg>
                  <p>
                    <span className="font-medium text-gray-700">Tip:</span> Add multiple schedules for different days. For example, you can set Monday-Friday 9AM-5PM and Saturday 10AM-2PM.
                  </p>
                </div>
              </div>

              {availabilityLoading ? (
                <div className="flex items-center justify-center py-8 bg-white rounded-2xl shadow-medium">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-dark"></div>
                  <span className="ml-3 text-sm text-gray-500">
                    {!professionalEntity?.id ? 'Initializing professional profile...' : 'Loading schedules...'}
                  </span>
                </div>
              ) : availabilityItems.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-8 bg-white rounded-2xl shadow-medium border-2 border-dashed border-gray-200">
                  <svg className="w-12 h-12 text-gray-300 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                  <p className="text-sm text-gray-500">No schedules configured yet</p>
                  <p className="text-xs text-gray-400 mt-1">Add your first working schedule above</p>
                </div>
              ) : (
                <div className="space-y-2">
                  <p className="text-sm font-medium text-gray-700 mb-3">Your Schedules</p>
                  {availabilityItems.map((item) => (
                    <div key={item.id} className="flex items-center justify-between p-4 bg-white rounded-xl shadow-sm hover:shadow-md transition-shadow border border-gray-100">
                      <div className="flex items-center gap-4">
                        <div className="w-10 h-10 rounded-full bg-primary-accent/10 flex items-center justify-center">
                          <svg className="w-5 h-5 text-primary-accent" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                          </svg>
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900">
                            {dayLabels[item.dayOfWeek] || 'Unknown day'}
                          </p>
                          <p className="text-xs text-gray-500">
                            {String(item.startTime).slice(0, 5)} - {String(item.endTime).slice(0, 5)}
                          </p>
                        </div>
                      </div>
                      <Button
                        type="button"
                        variant="ghost"
                        onClick={() => handleDeleteAvailability(item.id)}
                        className="p-2 hover:bg-red-50 rounded-lg transition-colors"
                      >
                        <Trash2 size={16} className="text-red-500" />
                      </Button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Submit */}
            <div className="flex gap-4">
              <Button type="submit" variant="primary" disabled={saving} className="flex-1">
                {saving ? (
                  'Saving...'
                ) : (
                  <>
                    <Save size={20} className="mr-2" />
                    {profile ? 'Update Profile' : 'Create Profile'}
                  </>
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </MainContent>
  )
}
