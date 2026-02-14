# Backend Integration Guide

## Overview

This document explains how to integrate the UI components with your appointment app backend.

## API Integration Points

### 1. Bookings API

#### Fetch Bookings
```javascript
// src/utils/api.js
const API_BASE_URL = 'http://localhost:5000/api'

export const bookingsApi = {
  // Get all bookings for current user
  getAll: async () => {
    const response = await fetch(`${API_BASE_URL}/bookings`)
    return response.json()
  },

  // Get bookings by status
  getByStatus: async (status) => {
    const response = await fetch(`${API_BASE_URL}/bookings?status=${status}`)
    return response.json()
  },

  // Get booking by ID
  getById: async (id) => {
    const response = await fetch(`${API_BASE_URL}/bookings/${id}`)
    return response.json()
  },

  // Cancel booking
  cancel: async (id) => {
    const response = await fetch(`${API_BASE_URL}/bookings/${id}/cancel`, {
      method: 'PATCH'
    })
    return response.json()
  },

  // Reschedule booking
  reschedule: async (id, newDateTime) => {
    const response = await fetch(`${API_BASE_URL}/bookings/${id}/reschedule`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newDateTime })
    })
    return response.json()
  }
}
```

#### Usage in BookingList
```jsx
import { useState, useEffect } from 'react'
import { BookingList } from '../components/booking/BookingList'
import { bookingsApi } from '../utils/api'

export const Bookings = () => {
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [activeTab, setActiveTab] = useState('upcoming')

  useEffect(() => {
    loadAppointments()
  }, [activeTab])

  const loadAppointments = async () => {
    try {
      const data = await bookingsApi.getByStatus(activeTab)
      setAppointments(data)
    } catch (error) {
      console.error('Error loading bookings:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleCancel = async (appointment) => {
    try {
      await bookingsApi.cancel(appointment.id)
      await loadAppointments()
      alert('Booking cancelled successfully')
    } catch (error) {
      console.error('Error cancelling booking:', error)
      alert('Failed to cancel booking')
    }
  }

  const handleReschedule = async (appointment) => {
    // Implement reschedule dialog
    const newDateTime = prompt('Enter new date and time:')
    if (newDateTime) {
      try {
        await bookingsApi.reschedule(appointment.id, newDateTime)
        await loadAppointments()
        alert('Booking rescheduled successfully')
      } catch (error) {
        console.error('Error rescheduling:', error)
        alert('Failed to reschedule booking')
      }
    }
  }

  return (
    <BookingList
      appointments={appointments}
      loading={loading}
      onReschedule={handleReschedule}
      onCancel={handleCancel}
    />
  )
}
```

### 2. Doctors API

#### Fetch Doctors
```javascript
// src/utils/api.js
export const doctorsApi = {
  // Get all doctors
  getAll: async () => {
    const response = await fetch(`${API_BASE_URL}/doctors`)
    return response.json()
  },

  // Get doctors by specialty
  getBySpecialty: async (specialty) => {
    const response = await fetch(`${API_BASE_URL}/doctors?specialty=${specialty}`)
    return response.json()
  },

  // Get doctor by ID
  getById: async (id) => {
    const response = await fetch(`${API_BASE_URL}/doctors/${id}`)
    return response.json()
  },

  // Search doctors
  search: async (query) => {
    const response = await fetch(`${API_BASE_URL}/doctors/search?q=${query}`)
    return response.json()
  },

  // Book appointment
  book: async (doctorId, appointmentData) => {
    const response = await fetch(`${API_BASE_URL}/appointments/book`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ doctorId, ...appointmentData })
    })
    return response.json()
  }
}
```

#### Usage in DoctorList
```jsx
import { useState, useEffect } from 'react'
import { DoctorList } from '../components/DoctorList'
import { doctorsApi } from '../utils/api'

export const DoctorListPage = () => {
  const [doctors, setDoctors] = useState([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')

  useEffect(() => {
    loadDoctors()
  }, [searchQuery])

  const loadDoctors = async () => {
    try {
      let data
      if (searchQuery) {
        data = await doctorsApi.search(searchQuery)
      } else {
        data = await doctorsApi.getAll()
      }
      setDoctors(data)
    } catch (error) {
      console.error('Error loading doctors:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleBook = async (doctorId) => {
    const doctor = doctors.find(d => d.id === doctorId)
    const appointmentDate = prompt('Enter appointment date (YYYY-MM-DD):')
    const appointmentTime = prompt('Enter appointment time (HH:mm):')

    if (appointmentDate && appointmentTime) {
      try {
        await doctorsApi.book(doctorId, {
          date: appointmentDate,
          time: appointmentTime
        })
        alert('Appointment booked successfully!')
      } catch (error) {
        console.error('Error booking:', error)
        alert('Failed to book appointment')
      }
    }
  }

  return (
    <DoctorList
      doctors={doctors}
      loading={loading}
      onBook={handleBook}
    />
  )
}
```

### 3. Profile API

#### Fetch User Profile
```javascript
// src/utils/api.js
export const profileApi = {
  // Get current user profile
  getProfile: async () => {
    const response = await fetch(`${API_BASE_URL}/users/profile`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
    return response.json()
  },

  // Update user profile
  updateProfile: async (profileData) => {
    const response = await fetch(`${API_BASE_URL}/users/profile`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      },
      body: JSON.stringify(profileData)
    })
    return response.json()
  },

  // Get user's favorite doctors
  getFavorites: async () => {
    const response = await fetch(`${API_BASE_URL}/users/favorites`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
    return response.json()
  },

  // Add/Remove favorite doctor
  toggleFavorite: async (doctorId) => {
    const response = await fetch(`${API_BASE_URL}/users/favorites/${doctorId}`, {
      method: 'PATCH',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
    return response.json()
  }
}
```

#### Usage in Profile Page
```jsx
import { useState, useEffect } from 'react'
import { Profile } from '../components/Profile'
import { profileApi } from '../utils/api'

export const ProfilePage = () => {
  const [user, setUser] = useState(null)
  const [favorites, setFavorites] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadProfile()
  }, [])

  const loadProfile = async () => {
    try {
      const [userData, favData] = await Promise.all([
        profileApi.getProfile(),
        profileApi.getFavorites()
      ])
      setUser(userData)
      setFavorites(favData)
    } catch (error) {
      console.error('Error loading profile:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleUpdateProfile = async (updatedData) => {
    try {
      await profileApi.updateProfile(updatedData)
      setUser(updatedData)
      alert('Profile updated successfully')
    } catch (error) {
      console.error('Error updating profile:', error)
      alert('Failed to update profile')
    }
  }

  const handleToggleFavorite = async (doctorId) => {
    try {
      await profileApi.toggleFavorite(doctorId)
      // Update local state if doctor was in favorites
      setFavorites(prev => {
        const exists = prev.find(f => f.doctorId === doctorId)
        if (exists) {
          return prev.filter(f => f.doctorId !== doctorId)
        } else {
          return [...prev, { doctorId }]
        }
      })
    } catch (error) {
      console.error('Error toggling favorite:', error)
    }
  }

  if (loading) {
    return <div>Loading...</div>
  }

  return (
    <Profile
      user={user}
      favorites={favorites}
      onUpdateProfile={handleUpdateProfile}
      onToggleFavorite={handleToggleFavorite}
    />
  )
}
```

## Authentication

### Token Management
```javascript
// src/utils/auth.js
export const authService = {
  // Store token in localStorage
  setToken: (token) => {
    localStorage.setItem('authToken', token)
  },

  // Get token from localStorage
  getToken: () => {
    return localStorage.getItem('authToken')
  },

  // Clear token
  clearToken: () => {
    localStorage.removeItem('authToken')
  },

  // Check if user is authenticated
  isAuthenticated: () => {
    return !!localStorage.getItem('authToken')
  },

  // Decode JWT token
  decodeToken: (token) => {
    try {
      const base64Url = token.split('.')[1]
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
      return JSON.parse(atob(base64))
    } catch (error) {
      return null
    }
  }
}
```

### Axios Instance with Auth
```javascript
// src/utils/api.js
import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json'
  }
})

// Add token to all requests
api.interceptors.request.use(
  (config) => {
    const token = authService.getToken()
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Handle 401 errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      authService.clearToken()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export const bookingsApi = {
  getAll: async () => api.get('/bookings'),
  getById: async (id) => api.get(`/bookings/${id}`),
  cancel: async (id) => api.patch(`/bookings/${id}/cancel`),
  reschedule: async (id, newDateTime) => 
    api.patch(`/bookings/${id}/reschedule`, { newDateTime })
}

// Export api instance for other requests
export default api
```

## State Management (Optional)

If you want to use React Query for better data fetching:

```bash
npm install @tanstack/react-query
```

```javascript
// src/utils/queryClient.js
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1
    }
  }
})
```

```javascript
// src/pages/Bookings.jsx
import { useQuery } from '@tanstack/react-query'
import { bookingsApi } from '../utils/api'

export const Bookings = () => {
  const [activeTab, setActiveTab] = useState('upcoming')

  const { data: appointments, isLoading } = useQuery({
    queryKey: ['bookings', activeTab],
    queryFn: () => bookingsApi.getByStatus(activeTab)
  })

  // ... rest of the component
}
```

## Error Handling

Create a custom error handler:

```javascript
// src/utils/errorHandler.js
export const handleApiError = (error) => {
  console.error('API Error:', error)
  
  const message = error.response?.data?.message || 
                 error.message || 
                 'An error occurred. Please try again.'
  
  return {
    message,
    status: error.response?.status
  }
}
```

## Loading States

Use different loading states for better UX:

```jsx
import { Button } from '../components/ui/Button'
import { Spinner } from './Spinner'

export const BookingList = ({ appointments, loading }) => {
  if (loading) {
    return (
      <div className="space-y-4">
        {[1, 2, 3].map((item) => (
          <BookingCardSkeleton key={item} />
        ))}
      </div>
    )
  }

  // ... rest of component
}

export const Spinner = () => (
  <div className="flex items-center justify-center py-8">
    <div className="w-6 h-6 border-2 border-gray-300 border-t-gray-600 rounded-full animate-spin" />
  </div>
)
```

## Testing Integration

```javascript
// Example test
describe('Bookings Component', () => {
  it('should fetch and display bookings', async () => {
    const mockBookings = [
      { id: 1, doctorName: 'Dr. John', status: 'upcoming' }
    ]

    jest.spyOn(bookingsApi, 'getByStatus').mockResolvedValue(mockBookings)
    
    render(<Bookings />)
    
    await waitFor(() => {
      expect(screen.getByText('Dr. John')).toBeInTheDocument()
    })
  })
})
```

## Security Considerations

1. **Never expose tokens in client-side code**
2. **Validate all API responses**
3. **Implement rate limiting**
4. **Use HTTPS in production**
5. **Sanitize all user inputs**
6. **Implement proper error logging**

## Future Enhancements

1. **Real-time updates** using WebSocket
2. **Push notifications** for appointment reminders
3. **Offline support** with Service Workers
4. **Analytics** for user behavior tracking
5. **Internationalization** (i18n) support
