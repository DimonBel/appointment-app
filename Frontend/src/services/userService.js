import { requestWithAuthRetry } from './httpClient'

const getIdentityApiBase = () => {
  const configured = import.meta.env.VITE_IDENTITY_API_URL
  if (!configured) return '/api'
  return configured.endsWith('/auth') ? configured.replace(/\/auth$/, '') : configured
}

class UserService {
  async getUserById(userId, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${identityApiBase}/users/${userId}`,
      },
      token
    )
    return response.data
  }

  async updateUser(user, token) {
    const identityApiBase = getIdentityApiBase()

    const payload = {
      id: user.id,
      email: user.email || '',
      userName: user.userName || '',
      firstName: user.firstName || null,
      lastName: user.lastName || null,
      avatarUrl: user.avatarUrl || null,
      phoneNumber: user.phoneNumber || null,
      isActive: typeof user.isActive === 'boolean' ? user.isActive : true,
      isOnline: typeof user.isOnline === 'boolean' ? user.isOnline : false,
      createdAt: user.createdAt || new Date().toISOString(),
      lastLoginAt: user.lastLoginAt || null,
      roles: Array.isArray(user.roles) ? user.roles : [],
    }

    await requestWithAuthRetry(
      {
        method: 'put',
        url: `${identityApiBase}/users/${user.id}`,
        data: payload,
      },
      token
    )

    return this.getUserById(user.id, token)
  }

  async uploadAvatar(userId, file, token) {
    const identityApiBase = getIdentityApiBase()
    const formData = new FormData()
    formData.append('avatar', file)

    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${identityApiBase}/users/${userId}/avatar`,
        data: formData,
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      },
      token
    )

    return response.data
  }
}

export const userService = new UserService()
