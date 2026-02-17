import axios from 'axios'
import { store } from '../store'
import { setCredentials, logout } from '../store/slices/authSlice'

const REFRESH_URL = '/api/auth/refresh'

const mergeHeaders = (headers = {}, token, userId = null) => ({
  ...headers,
  ...(token ? { Authorization: `Bearer ${token}` } : {}),
  ...(userId ? { 'X-User-Id': userId } : {}),
})

const tryRefreshToken = async () => {
  const state = store.getState()
  const accessToken = state.auth?.token
  const refreshToken = state.auth?.refreshToken

  if (!refreshToken) {
    return null
  }

  try {
    const response = await axios.post(REFRESH_URL, {
      accessToken: accessToken || '',
      refreshToken,
    })

    const payload = response.data
    if (!payload?.accessToken) {
      return null
    }

    store.dispatch(
      setCredentials({
        user: payload.user || state.auth?.user,
        accessToken: payload.accessToken,
        refreshToken: payload.refreshToken || refreshToken,
      })
    )

    return payload.accessToken
  } catch {
    store.dispatch(logout())
    return null
  }
}

export const requestWithAuthRetry = async (config, explicitToken = null) => {
  const state = store.getState()
  const token = explicitToken || state.auth?.token
  const userId = state.auth?.user?.id || null

  const requestConfig = {
    ...config,
    headers: mergeHeaders(config.headers, token, userId),
  }

  try {
    return await axios(requestConfig)
  } catch (error) {
    if (error?.response?.status !== 401) {
      throw error
    }

    const refreshedToken = await tryRefreshToken()
    if (!refreshedToken) {
      throw error
    }

    const retryConfig = {
      ...config,
      headers: mergeHeaders(config.headers, refreshedToken, userId),
    }

    return await axios(retryConfig)
  }
}
