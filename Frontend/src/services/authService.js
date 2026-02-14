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

  async register(email, password, firstName, lastName) {
    // Generate username from email (part before @)
    const userName = email.split('@')[0]
    
    const response = await axios.post(`${API_URL}/register`, {
      email,
      password,
      userName,
      firstName,
      lastName,
    })
    return response.data
  }

  async refreshToken(refreshToken) {
    const response = await axios.post(`${API_URL}/refresh`, {
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
}

export const authService = new AuthService()
