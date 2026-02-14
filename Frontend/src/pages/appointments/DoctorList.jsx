import React, { useState, useEffect } from 'react'
import { useSelector } from 'react-redux'
import { MainContent, SectionHeader } from '../../components/layout/MainContent'
import { Card, CardContent } from '../../components/ui/Card'
import { Button } from '../../components/ui/Button'
import { Avatar } from '../../components/ui/Avatar'
import { Input } from '../../components/ui/Input'
import { Loader } from '../../components/ui/Loader'
import { appointmentService } from '../../services/appointmentService'
import { Search, Star, MapPin, Calendar } from 'lucide-react'

export const DoctorList = () => {
  const [doctors, setDoctors] = useState([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedSpecialty, setSelectedSpecialty] = useState('all')
  const token = useSelector((state) => state.auth.token)

  useEffect(() => {
    fetchDoctors()
  }, [token])

  const fetchDoctors = async () => {
    setLoading(true)
    try {
      const data = await appointmentService.getProfessionals(token)
      setDoctors(data)
    } catch (error) {
      console.error('Error fetching doctors:', error)
      setDoctors([])
    } finally {
      setLoading(false)
    }
  }

  const specialties = [
    { value: 'all', label: 'All Specialties' },
    { value: 'cardiology', label: 'Cardiology' },
    { value: 'dermatology', label: 'Dermatology' },
    { value: 'neurology', label: 'Neurology' },
    { value: 'orthopedics', label: 'Orthopedics' },
    { value: 'pediatrics', label: 'Pediatrics' },
  ]

  const filteredDoctors = doctors.filter(doctor => {
    const matchesSearch = doctor.name?.toLowerCase().includes(searchQuery.toLowerCase()) ||
                         doctor.specialty?.toLowerCase().includes(searchQuery.toLowerCase())
    const matchesSpecialty = selectedSpecialty === 'all' || doctor.specialty === selectedSpecialty
    return matchesSearch && matchesSpecialty
  })

  return (
    <MainContent>
      <SectionHeader 
        title="Find Doctors"
        subtitle="Search and book appointments with healthcare professionals"
      />

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
          {filteredDoctors.map((doctor) => (
            <Card key={doctor.id} hover>
              <CardContent className="p-6">
                <div className="flex gap-4">
                  <Avatar 
                    src={doctor.avatar}
                    alt={doctor.name}
                    size={64}
                  />
                  
                  <div className="flex-1">
                    <h3 className="font-semibold text-text-primary text-lg mb-1">
                      {doctor.name}
                    </h3>
                    <p className="text-text-secondary text-sm mb-2">{doctor.specialty}</p>
                    
                    <div className="flex items-center gap-4 text-sm text-text-muted mb-3">
                      <div className="flex items-center gap-1">
                        <Star size={14} className="text-yellow-500 fill-yellow-500" />
                        <span>{doctor.rating || '4.8'}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <MapPin size={14} />
                        <span>{doctor.location || 'City Hospital'}</span>
                      </div>
                    </div>
                    
                    <Button size="sm" variant="primary" className="w-full">
                      <Calendar size={16} className="mr-2" />
                      Book Appointment
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </MainContent>
  )
}
