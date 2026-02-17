import { requestWithAuthRetry } from './httpClient'

const getIdentityApiBase = () => {
  const configured = import.meta.env.VITE_IDENTITY_API_URL
  if (!configured) return '/api'
  return configured.endsWith('/auth') ? configured.replace(/\/auth$/, '') : configured
}

class AdminService {
  async getAllUsers(token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${identityApiBase}/admin/users`,
      },
      token
    )
    return response.data
  }

  async getUserStatistics(token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'get',
        url: `${identityApiBase}/admin/statistics`,
      },
      token
    )
    return response.data
  }

  async createUser(userData, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${identityApiBase}/admin/users`,
        data: userData,
      },
      token
    )
    return response.data
  }

  async updateUser(userId, userData, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'put',
        url: `${identityApiBase}/admin/users/${userId}`,
        data: userData,
      },
      token
    )
    return response.data
  }

  async deleteUser(userId, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'delete',
        url: `${identityApiBase}/admin/users/${userId}`,
      },
      token
    )
    return response.data
  }

  async toggleUserActiveStatus(userId, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'patch',
        url: `${identityApiBase}/admin/users/${userId}/toggle-status`,
      },
      token
    )
    return response.data
  }

  async assignRole(userId, roleName, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${identityApiBase}/admin/users/${userId}/roles`,
        data: roleName,
        headers: {
          'Content-Type': 'application/json',
        },
      },
      token
    )
    return response.data
  }

  async removeRole(userId, roleName, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'delete',
        url: `${identityApiBase}/admin/users/${userId}/roles/${encodeURIComponent(roleName)}`,
      },
      token
    )
    return response.data
  }

  async resetUserPassword(userId, newPassword, token) {
    const identityApiBase = getIdentityApiBase()
    const response = await requestWithAuthRetry(
      {
        method: 'post',
        url: `${identityApiBase}/admin/users/${userId}/reset-password`,
        data: newPassword,
        headers: {
          'Content-Type': 'application/json',
        },
      },
      token
    )
    return response.data
  }
}

export const adminService = new AdminService()