import { TABS } from './constants'

export const mockAppointments = [
  {
    id: 1,
    doctorId: 1,
    doctorName: 'Dr. Sarah Johnson',
    specialty: 'Cardiologist',
    clinic: 'Heart Care Center',
    location: '123 Medical Ave, New York',
    date: '2026-02-15',
    time: '10:00 AM',
    status: TABS.UPCOMING,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Sarah'
  },
  {
    id: 2,
    doctorId: 2,
    doctorName: 'Dr. Michael Chen',
    specialty: 'Dermatologist',
    clinic: 'Skin Health Clinic',
    location: '456 Wellness Blvd, Los Angeles',
    date: '2026-02-20',
    time: '2:30 PM',
    status: TABS.UPCOMING,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Michael'
  },
  {
    id: 3,
    doctorId: 3,
    doctorName: 'Dr. Emily Davis',
    specialty: 'Pediatrician',
    clinic: 'Kids Health Center',
    location: '789 Pediatrics Street, Chicago',
    date: '2026-02-10',
    time: '9:00 AM',
    status: TABS.COMPLETED,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Emily'
  },
  {
    id: 4,
    doctorId: 4,
    doctorName: 'Dr. James Wilson',
    specialty: 'Orthopedic Surgeon',
    clinic: 'Bone & Joint Institute',
    location: '321 Spine Road, Boston',
    date: '2026-02-08',
    time: '11:30 AM',
    status: TABS.CANCELED,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=James'
  },
  {
    id: 5,
    doctorId: 5,
    doctorName: 'Dr. Lisa Anderson',
    specialty: 'Neurologist',
    clinic: 'Brain & Mind Center',
    location: '654 Neuro Lane, Seattle',
    date: '2026-02-25',
    time: '3:00 PM',
    status: TABS.UPCOMING,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Lisa'
  },
  {
    id: 6,
    doctorId: 1,
    doctorName: 'Dr. Sarah Johnson',
    specialty: 'Cardiologist',
    clinic: 'Heart Care Center',
    location: '123 Medical Ave, New York',
    date: '2026-02-05',
    time: '4:00 PM',
    status: TABS.COMPLETED,
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Sarah'
  }
]

export const mockDoctors = [
  {
    id: 1,
    name: 'Dr. Sarah Johnson',
    specialty: 'Cardiologist',
    rating: 4.8,
    reviews: 124,
    experience: '15 years',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Sarah',
    location: '123 Medical Ave, New York',
    availability: 'Mon, Wed, Fri'
  },
  {
    id: 2,
    name: 'Dr. Michael Chen',
    specialty: 'Dermatologist',
    rating: 4.9,
    reviews: 89,
    experience: '10 years',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Michael',
    location: '456 Wellness Blvd, Los Angeles',
    availability: 'Tue, Thu, Sat'
  },
  {
    id: 3,
    name: 'Dr. Emily Davis',
    specialty: 'Pediatrician',
    rating: 4.7,
    reviews: 156,
    experience: '8 years',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Emily',
    location: '789 Pediatrics Street, Chicago',
    availability: 'Mon, Wed, Fri'
  },
  {
    id: 4,
    name: 'Dr. James Wilson',
    specialty: 'Orthopedic Surgeon',
    rating: 4.6,
    reviews: 98,
    experience: '12 years',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=James',
    location: '321 Spine Road, Boston',
    availability: 'Tue, Thu'
  },
  {
    id: 5,
    name: 'Dr. Lisa Anderson',
    specialty: 'Neurologist',
    rating: 4.8,
    reviews: 112,
    experience: '14 years',
    avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=Lisa',
    location: '654 Neuro Lane, Seattle',
    availability: 'Mon, Wed, Fri'
  }
]

export const mockUser = {
  name: 'John Doe',
  email: 'john.doe@example.com',
  phone: '+1 234 567 890',
  avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=John',
  notifications: 5
}
