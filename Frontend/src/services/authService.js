import axios from 'axios'

const API_URL = import.meta.env.VITE_IDENTITY_API_URL || '/api/auth'

class AuthService {
  async login(email, password) {
    const response = await axios.post(`${API_URL}/login`, {
      email,
      password,
    })
    return response.data
  }

  async register(email, password, firstName, lastName, role = 'User', avatarFile = null) {
    // Generate username from email and ensure backend min length (3)
    const emailPrefix = (email.split('@')[0] || '').replace(/[^a-zA-Z0-9._-]/g, '')
    const userName = emailPrefix.length >= 3 ? emailPrefix : `user${Math.random().toString(36).slice(2, 8)}`

    const formData = new FormData()
    formData.append('email', email)
    formData.append('password', password)
    formData.append('userName', userName)
    formData.append('firstName', firstName || '')
    formData.append('lastName', lastName || '')
    formData.append('role', role)

    if (avatarFile) {
      formData.append('avatar', avatarFile)
    }

    // Don't set Content-Type for FormData - let Axios handle it automatically with proper boundary
    const response = await axios.post(`${API_URL}/register-with-avatar`, formData)
    return response.data
  }

  async refreshToken(refreshToken) {
    const accessToken = localStorage.getItem('token') || ''

    const response = await axios.post(`${API_URL}/refresh`, {
      accessToken,
      refreshToken,
    })
    return response.data
  }

  async logout(refreshToken, accessToken) {
    const response = await axios.post(
      `${API_URL}/revoke`,
      { refreshToken },
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      }
    )
    return response.data
  }

  async getCurrentUser(accessToken) {
    const response = await axios.get(`${API_URL}/me`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    })
    return response.data
  }

  async confirmEmail(userId, token) {
    const response = await axios.get(`${API_URL}/confirm-email`, {
      params: { userId, token },
    })
    return response.data
  }
}

export const authService = new AuthService()
