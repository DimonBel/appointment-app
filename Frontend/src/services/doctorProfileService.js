import axios from 'axios'

const PROFESSIONALS_BASE_URL = '/api/professionals'
const DOCTOR_PROFILES_BASE_URL = '/api/doctor-profiles'

class DoctorProfileService {
  // Get all doctor profiles (public)
  async getAllProfiles() {
    const response = await axios.get(DOCTOR_PROFILES_BASE_URL)
    return response.data
  }

  // Get profile by ID (public)
  async getProfileById(id) {
    const response = await axios.get(`${DOCTOR_PROFILES_BASE_URL}/${id}`)
    return response.data
  }

  // Get profile by user ID (public)
  async getProfileByUserId(userId) {
    const response = await axios.get(`${DOCTOR_PROFILES_BASE_URL}/user/${userId}`)
    return response.data
  }

  // Get profiles by specialty (public)
  async getProfilesBySpecialty(specialty) {
    const response = await axios.get(`${DOCTOR_PROFILES_BASE_URL}/specialty/${specialty}`)
    return response.data
  }

  // Search profiles (public)
  async searchProfiles(query) {
    const response = await axios.get(`${DOCTOR_PROFILES_BASE_URL}/search?query=${encodeURIComponent(query)}`)
    return response.data
  }

  // Get my professional profile from Professionals table (authenticated)
  async getMyProfessionalProfile(token) {
    const response = await axios.get(`${PROFESSIONALS_BASE_URL}/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Create professional profile (authenticated)
  async createProfessionalProfile(profileData, token) {
    // Map field names to match backend expectations
    const mappedData = {
      userId: profileData.userId, // This is REQUIRED by the backend
      title: profileData.title || 'Dr.',
      qualifications: profileData.qualifications,
      specialization: profileData.specialty || profileData.specialization,
      hourlyRate: profileData.consultationFee,
      experienceYears: profileData.yearsOfExperience,
      bio: profileData.bio,
    }

    console.log('[createProfessionalProfile] Input profileData:', profileData)
    console.log('[createProfessionalProfile] Mapped data:', mappedData)

    // Validate that userId is present
    if (!mappedData.userId) {
      throw new Error('userId is required to create a professional profile')
    }

    // Only include fields that are actually provided (not null/undefined/empty string)
    const cleanData = {}
    Object.keys(mappedData).forEach(key => {
      if (mappedData[key] !== undefined && mappedData[key] !== null && mappedData[key] !== '') {
        cleanData[key] = mappedData[key]
      }
    })

    console.log('[createProfessionalProfile] Clean data to send:', cleanData)
    console.log('[createProfessionalProfile] API URL:', PROFESSIONALS_BASE_URL)

    const response = await axios.post(PROFESSIONALS_BASE_URL, cleanData, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    console.log('[createProfessionalProfile] Response:', response.data)
    return response.data
  }

  // Update professional profile (authenticated) - uses PUT /api/professionals/{id}
  async updateProfessionalProfile(professionalId, profileData, token) {
    // Map field names to match backend expectations
    const mappedData = {
      title: profileData.title || 'Dr.',
      qualifications: profileData.qualifications,
      specialization: profileData.specialty || profileData.specialization,
      hourlyRate: profileData.consultationFee,
      experienceYears: profileData.yearsOfExperience,
      bio: profileData.bio,
    }

    console.log('[updateProfessionalProfile] Input profileData:', profileData)
    console.log('[updateProfessionalProfile] Mapped data:', mappedData)

    // Only include fields that are actually provided (not null/undefined/empty string)
    // This allows partial updates
    const cleanData = {}
    Object.keys(mappedData).forEach(key => {
      if (mappedData[key] !== undefined && mappedData[key] !== null && mappedData[key] !== '') {
        cleanData[key] = mappedData[key]
      }
    })

    console.log('[updateProfessionalProfile] Clean data to send:', cleanData)
    console.log('[updateProfessionalProfile] API URL:', `${PROFESSIONALS_BASE_URL}/${professionalId}`)

    const response = await axios.put(`${PROFESSIONALS_BASE_URL}/${professionalId}`, cleanData, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    console.log('[updateProfessionalProfile] Response:', response.data)
    return response.data
  }

  // Get my profile (authenticated) - legacy method, redirects to Professionals API
  async getMyProfile(token) {
    try {
      return await this.getMyProfessionalProfile(token)
    } catch (error) {
      // If Professionals API doesn't exist, try Doctor Profiles API
      const response = await axios.get(`${DOCTOR_PROFILES_BASE_URL}/my-profile`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      return response.data
    }
  }

  // Create profile (authenticated) - legacy method
  async createProfile(profileData, token) {
    return await this.createProfessionalProfile(profileData, token)
  }

  // Update profile (authenticated) - legacy method
  async updateProfile(profileData, token) {
    // First, get the professional ID
    const professional = await this.getMyProfessionalProfile(token)
    console.log('[updateProfile] Professional found:', professional)
    if (professional?.id) {
      console.log('[updateProfile] Updating professional:', professional.id, profileData)
      return await this.updateProfessionalProfile(professional.id, profileData, token)
    }
    throw new Error('Professional profile not found. Please ensure your professional profile has been created in the Appointment service.')
  }

  // Delete profile (authenticated) - legacy method
  async deleteProfile(token) {
    try {
      const response = await axios.delete(`${PROFESSIONALS_BASE_URL}/me`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      return response.data
    } catch (error) {
      const response = await axios.delete(DOCTOR_PROFILES_BASE_URL, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      return response.data
    }
  }
}

export const doctorProfileService = new DoctorProfileService()
