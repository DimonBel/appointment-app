import axios from 'axios'

const BASE_URL = '/api/doctor-profiles'

class DoctorProfileService {
  // Get all doctor profiles (public)
  async getAllProfiles() {
    const response = await axios.get(BASE_URL)
    return response.data
  }

  // Get profile by ID (public)
  async getProfileById(id) {
    const response = await axios.get(`${BASE_URL}/${id}`)
    return response.data
  }

  // Get profile by user ID (public)
  async getProfileByUserId(userId) {
    const response = await axios.get(`${BASE_URL}/user/${userId}`)
    return response.data
  }

  // Get profiles by specialty (public)
  async getProfilesBySpecialty(specialty) {
    const response = await axios.get(`${BASE_URL}/specialty/${specialty}`)
    return response.data
  }

  // Search profiles (public)
  async searchProfiles(query) {
    const response = await axios.get(`${BASE_URL}/search?query=${encodeURIComponent(query)}`)
    return response.data
  }

  // Get my profile (authenticated)
  async getMyProfile(token) {
    const response = await axios.get(`${BASE_URL}/my-profile`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Create profile (authenticated)
  async createProfile(profileData, token) {
    const response = await axios.post(BASE_URL, profileData, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Update profile (authenticated)
  async updateProfile(profileData, token) {
    const response = await axios.put(BASE_URL, profileData, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }

  // Delete profile (authenticated)
  async deleteProfile(token) {
    const response = await axios.delete(BASE_URL, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
    return response.data
  }
}

export const doctorProfileService = new DoctorProfileService()
