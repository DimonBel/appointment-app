import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Avatar } from '../../components/ui/Avatar'
import { Loader } from '../../components/ui/Loader'
import { BookingModal } from '../../components/booking/BookingModal'
import { appointmentService } from '../../services/appointmentService'
import { userService } from '../../services/userService'
import { Search, MapPin, Calendar, Briefcase, DollarSign } from 'lucide-react'

export const DoctorList = () => {
  const [doctors, setDoctors] = useState([])
  const [loading, setLoading] = useState(true)
  const [bookingLoading, setBookingLoading] = useState(false)
  const [selectedDoctor, setSelectedDoctor] = useState(null)
  const [bookingModalOpen, setBookingModalOpen] = useState(false)
  const [bookingMessage, setBookingMessage] = useState('')
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedSpecialty, setSelectedSpecialty] = useState('all')
  const token = useSelector((state) => state.auth.token)

  useEffect(() => {
    fetchDoctors()
  }, [])

  const fetchDoctors = async () => {
    setLoading(true)
    try {
      // Get professionals from Appointment API
      const professionals = await appointmentService.getProfessionals(token)
      const professionalArray = Array.isArray(professionals) ? professionals : []

      const doctors = await Promise.all(
        professionalArray.map(async (prof) => {
          const fallbackUser = {
            id: prof.user?.id || prof.userId || null,
            firstName: prof.user?.firstName || null,
            lastName: prof.user?.lastName || null,
            userName: prof.user?.userName || null,
            email: prof.user?.email || null,
            avatarUrl: prof.user?.avatarUrl || null,
          }

          let resolvedUser = fallbackUser
          const userId = prof.userId || prof.user?.id

          if (token && userId) {
            try {
              const identityUser = await userService.getUserById(userId, token)
              if (identityUser) {
                resolvedUser = {
                  ...fallbackUser,
                  id: identityUser.id || fallbackUser.id,
                  firstName: identityUser.firstName || fallbackUser.firstName,
                  lastName: identityUser.lastName || fallbackUser.lastName,
                  userName: identityUser.userName || fallbackUser.userName,
                  email: identityUser.email || fallbackUser.email,
                  avatarUrl: identityUser.avatarUrl || fallbackUser.avatarUrl,
                }
              }
            } catch {
              // Keep fallbackUser when Identity lookup is unavailable
            }
          }

          return {
            id: prof.id,
            user: resolvedUser,
            specialty: prof.specialization,
            bio: prof.bio,
            qualifications: prof.qualifications,
            yearsOfExperience: prof.experienceYears,
            services: [],
            consultationFee: prof.hourlyRate,
            languages: [],
            city: null,
            country: null,
            address: null,
            isAvailableForAppointments: prof.isAvailable,
          }
        })
      )
      
      setDoctors(doctors)
    } catch (error) {
      console.error('Error fetching doctors:', error)
      setDoctors([])
    } finally {
      setLoading(false)
    }
  }

  // Extract unique specialties from doctors - ensure doctors is array
  const specialties = [
    { value: 'all', label: 'All Specialties' },
    ...(Array.isArray(doctors) ? 
      Array.from(new Set(doctors.map(d => d.specialty).filter(Boolean)))
        .map(specialty => ({ value: specialty, label: specialty })) : 
      [])
  ]

  const filteredDoctors = Array.isArray(doctors) ? doctors.filter(doctor => {
    const fullName = doctor.user?.firstName && doctor.user?.lastName
      ? `${doctor.user.firstName} ${doctor.user.lastName}`.toLowerCase()
      : (doctor.user?.userName || '').toLowerCase()
    
    const matchesSearch = 
      fullName.includes(searchQuery.toLowerCase()) ||
      (doctor.specialty?.toLowerCase() || '').includes(searchQuery.toLowerCase()) ||
      (doctor.city?.toLowerCase() || '').includes(searchQuery.toLowerCase())
    
    const matchesSpecialty = selectedSpecialty === 'all' || doctor.specialty === selectedSpecialty
    
    return matchesSearch && matchesSpecialty
  }) : []

  const openBookingModal = async (doctor) => {
    if (!token) {
      alert('Please login to book an appointment')
      return
    }

    if (!doctor?.user?.id) {
      alert('Doctor profile is incomplete. Please try another doctor.')
      return
    }

    try {
      setBookingLoading(true)
      let professional = await appointmentService.getProfessionalByUserId(doctor.user.id, token)

      if (!professional) {
        professional = await appointmentService.createProfessional({
          userId: doctor.user.id,
          title: 'Dr.',
          qualifications: doctor.qualifications || null,
          specialization: doctor.specialty || 'General',
        }, token)
      }

      setBookingMessage('')
      setSelectedDoctor({ ...doctor, professionalId: professional.id })
      setBookingModalOpen(true)
    } catch (error) {
      console.error('Error preparing booking:', error)
      alert(error.response?.data?.message || 'Failed to open booking form')
    } finally {
      setBookingLoading(false)
    }
  }

  const closeBookingModal = () => {
    setBookingModalOpen(false)
    setSelectedDoctor(null)
  }

  const handleConfirmBooking = async ({ scheduledDateTime, durationMinutes, notes }) => {
    if (!selectedDoctor || !selectedDoctor.professionalId) {
      alert('Doctor data is incomplete')
      return
    }

    try {
      setBookingLoading(true)

      const doctorName = selectedDoctor.user?.firstName && selectedDoctor.user?.lastName
        ? `${selectedDoctor.user.firstName} ${selectedDoctor.user.lastName}`
        : selectedDoctor.user?.userName || 'Doctor'

      await appointmentService.createOrder({
        professionalId: selectedDoctor.professionalId,
        scheduledDateTime,
        durationMinutes,
        title: `Appointment with Dr. ${doctorName}`,
        description: notes || `Consultation: ${selectedDoctor.specialty || 'General'}`,
        domainConfigurationId: null,
      }, token)

      setBookingMessage('Appointment created successfully!')
      closeBookingModal()
    } catch (error) {
      console.error('Error creating appointment:', error)
      alert(error.response?.data?.message || error.response?.data?.error || 'Failed to create appointment')
    } finally {
      setBookingLoading(false)
    }
  }

  return (
    <MainContent>
      <SectionHeader 
        title="Find Doctors"
        subtitle="Search and book appointments with healthcare professionals"
      />

      {bookingMessage && (
        <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700">
          {bookingMessage}
        </div>
      )}

      {/* Search and Filter */}
      <Card className="mb-6">
        <CardContent className="p-4">
          <div className="flex flex-col md:flex-row gap-4">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-text-muted" size={20} />
              <input
                type="text"
                placeholder="Search by name or specialty..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
              />
            </div>
            <select
              value={selectedSpecialty}
              onChange={(e) => setSelectedSpecialty(e.target.value)}
              className="px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-accent"
            >
              {specialties.map(spec => (
                <option key={spec.value} value={spec.value}>{spec.label}</option>
              ))}
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Doctors List */}
      {loading ? (
        <div className="flex justify-center py-12">
          <Loader size="lg" />
        </div>
      ) : filteredDoctors.length === 0 ? (
        <Card>
          <CardContent className="text-center py-12">
            <Search size={48} className="mx-auto text-text-muted mb-4" />
            <p className="text-text-secondary">No doctors found</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filteredDoctors.map((doctor) => {
            const doctorName = doctor.user?.firstName && doctor.user?.lastName
              ? `${doctor.user.firstName} ${doctor.user.lastName}`
              : doctor.user?.userName || 'Doctor'
            
            return (
              <Card key={doctor.id} hover>
                <CardContent className="p-6">
                  <div className="flex gap-4">
                    <Avatar 
                      src={doctor.user?.avatarUrl}
                      alt={doctorName}
                      size={64}
                    />
                    
                    <div className="flex-1">
                      <h3 className="font-semibold text-text-primary text-lg mb-1">
                        Dr. {doctorName}
                      </h3>
                      <p className="text-text-secondary text-sm mb-2">{doctor.specialty}</p>
                      
                      {doctor.bio && (
                        <p className="text-sm text-text-muted mb-3 line-clamp-2">
                          {doctor.bio}
                        </p>
                      )}
                      
                      <div className="flex flex-wrap gap-3 text-sm text-text-muted mb-3">
                        {doctor.yearsOfExperience > 0 && (
                          <div className="flex items-center gap-1">
                            <Briefcase size={14} />
                            <span>{doctor.yearsOfExperience} years exp.</span>
                          </div>
                        )}
                        {doctor.city && (
                          <div className="flex items-center gap-1">
                            <MapPin size={14} />
                            <span>{doctor.city}</span>
                          </div>
                        )}
                        {doctor.consultationFee && (
                          <div className="flex items-center gap-1">
                            <DollarSign size={14} />
                            <span>${doctor.consultationFee}</span>
                          </div>
                        )}
                      </div>

                      {doctor.services && doctor.services.length > 0 && (
                        <div className="mb-3">
                          <div className="flex flex-wrap gap-1">
                            {doctor.services.slice(0, 3).map((service, index) => (
                              <span
                                key={index}
                                className="px-2 py-1 bg-primary-accent/10 text-primary-accent rounded text-xs"
                              >
                                {service}
                              </span>
                            ))}
                            {doctor.services.length > 3 && (
                              <span className="px-2 py-1 text-text-muted rounded text-xs">
                                +{doctor.services.length - 3} more
                              </span>
                            )}
                          </div>
                        </div>
                      )}
                      
                      <Button size="sm" variant="primary" className="w-full" onClick={() => openBookingModal(doctor)}>
                        <Calendar size={16} className="mr-2" />
                        Book Appointment
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      )}

      <BookingModal
        isOpen={bookingModalOpen}
        doctor={selectedDoctor}
        loading={bookingLoading}
        onClose={closeBookingModal}
        onConfirm={handleConfirmBooking}
      />
    </MainContent>
  )
}
