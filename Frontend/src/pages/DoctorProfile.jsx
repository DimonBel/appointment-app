import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../components/layout/MainContent'
import { Card, CardContent } from '../components/ui/Card'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { Loader } from '../components/ui/Loader'
import { doctorProfileService } from '../services/doctorProfileService'
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
    workingHours: '',
    consultationFee: '',
    languages: [],
    address: '',
    city: '',
    country: '',
    isAvailableForAppointments: true,
  })
  const [newService, setNewService] = useState('')
  const [newLanguage, setNewLanguage] = useState('')

  const token = useSelector((state) => state.auth.token)

  useEffect(() => {
    if (token) {
      fetchProfile()
    } else {
      setLoading(false)
      setError('Please login to manage your professional profile')
    }
  }, [token])

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
          workingHours: '',
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
        workingHours: data.workingHours || '',
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
      } else {
        setError(err.response?.data?.message || 'Failed to load profile')
      }
    } finally {
      setLoading(false)
    }
  }

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
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to save profile')
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
                <Input
                  label="Working Hours"
                  name="workingHours"
                  value={formData.workingHours}
                  onChange={handleChange}
                  placeholder="e.g., Mon-Fri 9AM-5PM"
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
