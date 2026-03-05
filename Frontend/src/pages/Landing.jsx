import React, { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useSelector } from 'react-redux'
import { Button } from '../components/ui/Button'
import { Avatar } from '../components/ui/Avatar'
import { Loader } from '../components/ui/Loader'
import { STANDARD_SPECIALTIES } from '../utils/specialtyUtils'
import { DOCTORS } from '../data/doctors'
import { appointmentService } from '../services/appointmentService'
import { userService } from '../services/userService'
import { 
  Calendar, 
  Shield, 
  Users, 
  Clock, 
  CheckCircle,
  ArrowRight,
  Phone,
  Mail,
  MapPin,
  DollarSign,
  Briefcase
} from 'lucide-react'

// Pool of professional medical doctor images from Unsplash (all unique)
const MEDICAL_IMAGES = [
  'https://images.unsplash.com/photo-1559839734-2b71ea197ec2?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1594824476967-48c8b964273f?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1622253692010-333f2da6031d?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1551836022-d5d88e9218df?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1651008376811-b90baee60c1f?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1537368910025-700350fe46c7?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1582750433449-648ed127bb54?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1628151015968-3a44274e8d6f?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1551836022-deb4988cc6c0?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1587563871167-1ee9c731aef4?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1519345182560-3f2917c472ef?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1507591064344-4c6ce005b128?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1552058544-f2b08422138a?w=400&h=400&fit=crop&crop=face',
  'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=400&h=400&fit=crop&crop=face',
]

// Better hash function that uses all characters and multiplies for better distribution
const hashString = (str) => {
  let hash = 0
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i)
    hash = ((hash << 5) - hash) + char
    hash = hash & hash // Convert to 32bit integer
  }
  return Math.abs(hash)
}

// Map doctor names to professional images (fallback only)
const getDoctorImage = (name, userId, userAvatarUrl, specialty) => {
  // PRIORITY 1: Use the user's actual avatar from the database
  if (userAvatarUrl && userAvatarUrl.trim() !== '') {
    return userAvatarUrl
  }
  
  // PRIORITY 2: Assign unique medical image based on user ID (better hash)
  if (userId) {
    const hash = hashString(userId.toString())
    const imageIndex = hash % MEDICAL_IMAGES.length
    return MEDICAL_IMAGES[imageIndex]
  }
  
  // PRIORITY 3: Generate unique medical image based on doctor name hash
  if (name) {
    const hash = hashString(name)
    const imageIndex = hash % MEDICAL_IMAGES.length
    return MEDICAL_IMAGES[imageIndex]
  }
  
  // PRIORITY 4: Fallback to first medical image
  return MEDICAL_IMAGES[0]
}

// Format working hours from availabilities
const formatWorkingHours = (availabilities) => {
  if (!availabilities || availabilities.length === 0) {
    return null
  }

  // Group by time ranges
  const dayMap = {}
  const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']

  availabilities.forEach((avail) => {
    // Accept both Daily (0) and Weekly (1) schedules
    if (!avail.isActive || (avail.scheduleType !== 0 && avail.scheduleType !== 1)) return

    const dayName = dayNames[avail.dayOfWeek]
    const timeRange = `${formatTime(avail.startTime)}-${formatTime(avail.endTime)}`

    if (!dayMap[timeRange]) {
      dayMap[timeRange] = []
    }
    dayMap[timeRange].push(avail.dayOfWeek)
  })

  if (Object.keys(dayMap).length === 0) {
    return null
  }

  // Format consecutive days
  const result = []
  for (const [timeRange, days] of Object.entries(dayMap)) {
    days.sort((a, b) => a - b)
    const dayRange = formatDayRange(days)
    result.push(`${dayRange} ${timeRange}`)
  }

  return result.join(', ')
}

// Format time from TimeSpan (e.g., "09:00:00" -> "9:00")
const formatTime = (timeStr) => {
  if (!timeStr) return ''
  const [hours, minutes] = timeStr.split(':').map(Number)
  const h = hours === 0 ? 12 : hours > 12 ? hours - 12 : hours
  const ampm = hours < 12 ? 'AM' : 'PM'
  return `${h}:${minutes.toString().padStart(2, '0')} ${ampm}`
}

// Format day range (e.g., [1,2,3,4,5] -> "Mon-Fri")
const formatDayRange = (days) => {
  const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
  
  if (days.length === 0) return ''
  if (days.length === 1) return dayNames[days[0]]

  // Check if days are consecutive
  const isConsecutive = days.every((day, i) => i === 0 || day === days[i - 1] + 1)
  
  if (isConsecutive) {
    return `${dayNames[days[0]]}-${dayNames[days[days.length - 1]]}`
  }

  // If not consecutive, show as comma-separated
  return days.map(d => dayNames[d]).join(', ')
}

export const Landing = () => {
  const [doctors, setDoctors] = useState([])
  const [doctorsLoading, setDoctorsLoading] = useState(true)
  const token = useSelector((state) => state.auth.token)

  useEffect(() => {
    fetchDoctors()
  }, [])

  const fetchDoctors = async () => {
    setDoctorsLoading(true)
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

          const workingHours = formatWorkingHours(prof.availabilities || [])

          return {
            id: prof.id,
            user: resolvedUser,
            specialty: prof.specialization,
            bio: prof.bio,
            qualifications: prof.qualifications,
            yearsOfExperience: prof.experienceYears,
            consultationFee: prof.hourlyRate,
            languages: [],
            city: null,
            country: null,
            address: null,
            isAvailableForAppointments: prof.isAvailable,
            availabilities: prof.availabilities || [],
            workingHours: workingHours,
          }
        })
      )
      
      setDoctors(doctors)
    } catch (error) {
      console.error('Error fetching doctors:', error)
      setDoctors([])
    } finally {
      setDoctorsLoading(false)
    }
  }
  return (
    <div className="min-h-screen bg-white">
      {/* Hero Section */}
      <section className="relative bg-gradient-to-br from-primary-dark via-primary-dark to-primary-accent text-white py-20 px-6 overflow-hidden">
        <div className="max-w-6xl mx-auto relative z-10">
          <div className="max-w-3xl">
            <h1 className="text-5xl md:text-6xl font-bold mb-6 leading-tight">
              Your Health,<br />Our Priority
            </h1>
            <p className="text-xl md:text-2xl mb-8 text-white/90 leading-relaxed">
              Book appointments with top healthcare professionals across all specialties. Simple, fast, and reliable.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 relative z-20">
              <Link to="/register" className="relative z-20">
                <Button size="lg" variant="secondary" className="w-full sm:w-auto px-8 py-4 text-base">
                  Get Started
                  <ArrowRight size={20} className="ml-2" />
                </Button>
              </Link>
              <Link to="/login" className="relative z-20">
                <Button size="lg" variant="outline" className="w-full sm:w-auto px-8 py-4 text-base border-white text-white hover:bg-white/10">
                  Login
                </Button>
              </Link>
            </div>
          </div>
        </div>

        {/* Decorative Elements - positioned behind content */}
        <div className="absolute top-20 right-10 w-64 h-64 bg-white/5 rounded-full blur-3xl pointer-events-none z-0" />
        <div className="absolute bottom-10 left-10 w-96 h-96 bg-primary-accent/20 rounded-full blur-3xl pointer-events-none z-0" />
      </section>

      {/* Specialties Section */}
      <section className="py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              Our Medical Specialties
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Comprehensive healthcare services across all major medical fields
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {STANDARD_SPECIALTIES.map((specialty) => (
              <Link
                key={specialty.id}
                to="/register"
                className="group bg-white border border-gray-200 rounded-2xl p-6 hover:shadow-xl hover:border-primary-accent/50 transition-all duration-300"
              >
                <div className="text-5xl mb-4">{specialty.icon}</div>
                <h3 className="text-xl font-semibold text-text-primary mb-2 group-hover:text-primary-accent transition-colors">
                  {specialty.name}
                </h3>
                <p className="text-text-secondary text-sm leading-relaxed">
                  {specialty.description}
                </p>
                <div className="mt-4 flex items-center text-primary-accent text-sm font-medium opacity-0 group-hover:opacity-100 transition-opacity">
                  Book Now
                  <ArrowRight size={16} className="ml-1" />
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 px-6 bg-gray-50">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              Why Choose Us
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Modern healthcare with convenience at its core
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="text-center">
              <div className="w-16 h-16 bg-primary-accent/10 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Calendar size={32} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-text-primary mb-3">
                Easy Booking
              </h3>
              <p className="text-text-secondary leading-relaxed">
                Book appointments online in minutes. No phone calls, no waiting.
              </p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-primary-accent/10 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Shield size={32} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-text-primary mb-3">
                Verified Doctors
              </h3>
              <p className="text-text-secondary leading-relaxed">
                All healthcare professionals are verified and highly qualified.
              </p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-primary-accent/10 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Clock size={32} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-text-primary mb-3">
                24/7 Support
              </h3>
              <p className="text-text-secondary leading-relaxed">
                Get help anytime with our AI assistant and support team.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section className="py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              How It Works
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Simple steps to book your appointment
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="relative">
              <div className="bg-white border border-gray-200 rounded-2xl p-8 relative z-10">
                <div className="w-12 h-12 bg-primary-accent text-white rounded-full flex items-center justify-center text-xl font-bold mb-6">
                  1
                </div>
                <h3 className="text-xl font-semibold text-text-primary mb-3">
                  Choose a Doctor
                </h3>
                <p className="text-text-secondary leading-relaxed">
                  Browse our verified healthcare professionals by specialty
                </p>
              </div>
              <div className="hidden md:block absolute top-1/2 -right-4 w-8 h-0.5 bg-gray-200 transform -translate-y-1/2" />
            </div>

            <div className="relative">
              <div className="bg-white border border-gray-200 rounded-2xl p-8 relative z-10">
                <div className="w-12 h-12 bg-primary-accent text-white rounded-full flex items-center justify-center text-xl font-bold mb-6">
                  2
                </div>
                <h3 className="text-xl font-semibold text-text-primary mb-3">
                  Book Appointment
                </h3>
                <p className="text-text-secondary leading-relaxed">
                  Select your preferred date and time slot
                </p>
              </div>
              <div className="hidden md:block absolute top-1/2 -right-4 w-8 h-0.5 bg-gray-200 transform -translate-y-1/2" />
            </div>

            <div>
              <div className="bg-white border border-gray-200 rounded-2xl p-8">
                <div className="w-12 h-12 bg-primary-accent text-white rounded-full flex items-center justify-center text-xl font-bold mb-6">
                  3
                </div>
                <h3 className="text-xl font-semibold text-text-primary mb-3">
                  Get Confirmation
                </h3>
                <p className="text-text-secondary leading-relaxed">
                  Receive instant confirmation and appointment details
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

{/* Featured Doctors Section */}
      <section className="py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              Meet Our Top Doctors
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Highly qualified healthcare professionals ready to help you
            </p>
          </div>

          {doctorsLoading ? (
            <div className="flex justify-center py-12">
              <Loader size="lg" />
            </div>
          ) : doctors.length === 0 ? (
            <Card>
              <CardContent className="text-center py-12">
                <Users size={48} className="mx-auto text-text-muted mb-4" />
                <p className="text-text-secondary">No doctors available at the moment</p>
              </CardContent>
            </Card>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {doctors.slice(0, 8).map((doctor) => {
                const doctorName = doctor.user?.firstName && doctor.user?.lastName
                  ? `${doctor.user.firstName} ${doctor.user.lastName}`
                  : doctor.user?.userName || 'Doctor'
                
                return (
                  <Link
                    key={doctor.id}
                    to={token ? '/doctors' : '/register'}
                    className="group bg-white border border-gray-200 rounded-2xl overflow-hidden hover:shadow-xl hover:border-primary-accent/50 transition-all duration-300"
                  >
                    <div className="h-64 bg-gradient-to-br from-primary-accent/20 to-primary-dark/10 relative overflow-hidden">
                      <img
                        src={getDoctorImage(doctorName, doctor.user?.id || doctor.id, doctor.user?.avatarUrl, doctor.specialty)}
                        alt={doctorName}
                        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                      />
                      <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/60 to-transparent p-4">
                        <span className="text-white text-sm font-medium bg-primary-accent/90 px-3 py-1 rounded-full">
                          {doctor.user?.firstName?.[0]}{doctor.user?.lastName?.[0] || doctor.user?.userName?.[0]?.toUpperCase() || 'D'}
                        </span>
                      </div>
                    </div>
                    <div className="p-5">
                      <h3 className="font-semibold text-text-primary mb-1">Dr. {doctorName}</h3>
                      <p className="text-primary-accent text-sm font-medium mb-2">{doctor.specialty}</p>
                      
                      {doctor.bio && (
                        <p className="text-text-secondary text-sm line-clamp-2 mb-3">
                          {doctor.bio}
                        </p>
                      )}
                      
                      <div className="flex items-center gap-4 text-sm text-text-muted mb-3">
                        {doctor.yearsOfExperience > 0 && (
                          <div className="flex items-center gap-1">
                            <Briefcase size={14} />
                            <span>{doctor.yearsOfExperience} years</span>
                          </div>
                        )}
                        {doctor.consultationFee && (
                          <div className="flex items-center gap-1">
                            <DollarSign size={14} />
                            <span>${doctor.consultationFee}</span>
                          </div>
                        )}
                      </div>

                      {doctor.workingHours && (
                        <div className="flex items-center gap-1 text-sm text-text-muted mb-3">
                          <Clock size={14} />
                          <span>{doctor.workingHours}</span>
                        </div>
                      )}

                      <div className="flex items-center justify-between">
                        <span className="text-primary-accent text-sm font-medium opacity-0 group-hover:opacity-100 transition-opacity">
                          Book Now
                        </span>
                      </div>
                    </div>
                  </Link>
                )
              })}
            </div>
          )}

          <div className="text-center mt-10">
            <Link to={token ? '/doctors' : '/register'}>
              <Button size="lg" variant="primary" className="px-8 py-3">
                {token ? 'View All Doctors' : 'Create Free Account'}
                <ArrowRight size={18} className="ml-2" />
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 px-6 bg-primary-dark text-white">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-4xl md:text-5xl font-bold mb-6">
            Ready to Book Your Appointment?
          </h2>
          <p className="text-xl text-white/90 mb-8 max-w-2xl mx-auto">
            Join thousands of satisfied patients who trust us with their healthcare
          </p>
          <Link to="/register">
            <Button size="lg" variant="secondary" className="px-10 py-4 text-base">
              Create Free Account
              <ArrowRight size={20} className="ml-2" />
            </Button>
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-white py-12 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 mb-8">
            <div>
              <h3 className="text-xl font-bold mb-4">Contact Us</h3>
              <div className="space-y-3">
                <div className="flex items-center gap-3 text-gray-300">
                  <Phone size={18} />
                  <span>+373 22 123 456</span>
                </div>
                <div className="flex items-center gap-3 text-gray-300">
                  <Mail size={18} />
                  <span>info@appointment-app.md</span>
                </div>
                <div className="flex items-center gap-3 text-gray-300">
                  <MapPin size={18} />
                  <span>Chișinău, Moldova</span>
                </div>
              </div>
            </div>

            <div>
              <h3 className="text-xl font-bold mb-4">Quick Links</h3>
              <div className="space-y-2">
                <Link to="/register" className="block text-gray-300 hover:text-white transition-colors">
                  Register
                </Link>
                <Link to="/login" className="block text-gray-300 hover:text-white transition-colors">
                  Login
                </Link>
                <Link to="/register" className="block text-gray-300 hover:text-white transition-colors">
                  Find Doctors
                </Link>
              </div>
            </div>

            <div>
              <h3 className="text-xl font-bold mb-4">Working Hours</h3>
              <div className="space-y-2 text-gray-300">
                <p>Monday - Friday: 8:00 - 19:00</p>
                <p>Saturday: 8:00 - 13:00</p>
                <p>Sunday: Closed</p>
              </div>
            </div>
          </div>

          <div className="border-t border-gray-800 pt-8 text-center text-gray-400">
            <p>&copy; 2026 Appointment App. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  )
}