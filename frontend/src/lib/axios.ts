import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type { ApiResponse, ApiError } from '@/types'

const API_BASE_URL = import.meta.env.VITE_API_URL || '/api'

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  // IMPORTANT: Send cookies with requests (HttpOnly auth cookies)
  withCredentials: true,
  timeout: 30000,
})

// Request interceptor
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Cookies are sent automatically with withCredentials: true
    // No need to manually add Authorization header
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor - handle errors & token refresh
let isRefreshing = false
let failedQueue: Array<{
  resolve: (value: unknown) => void
  reject: (reason?: unknown) => void
}> = []

const processQueue = (error: Error | null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error)
    } else {
      promise.resolve(undefined)
    }
  })
  failedQueue = []
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiResponse<unknown>>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean
    }

    // Handle 429 Too Many Requests (Rate Limiting)
    if (error.response?.status === 429) {
      const retryAfter = error.response.headers['retry-after']
      window.dispatchEvent(
        new CustomEvent('api:rate-limited', {
          detail: { retryAfter: retryAfter ? parseInt(retryAfter, 10) : undefined },
        })
      )
      return Promise.reject(transformError(error))
    }

    // Handle 403 Forbidden - check for IP blacklist
    if (error.response?.status === 403) {
      const errorCode = error.response.data?.errors?.[0]?.code
      if (errorCode === 'IP_BLACKLISTED') {
        window.dispatchEvent(new CustomEvent('api:ip-blacklisted'))
        return Promise.reject(transformError(error))
      }
    }

    // Handle 401 Unauthorized - attempt token refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Check for force re-auth header (permissions changed, don't try refresh)
      const forceReauth = error.response.headers['x-force-reauth']
      if (forceReauth === 'true' || forceReauth === '1') {
        localStorage.removeItem('exoauth_has_session')
        window.dispatchEvent(new CustomEvent('auth:force-reauth'))
        return Promise.reject(transformError(error))
      }

      // Don't try to refresh for login/register/refresh endpoints (would cause infinite loop)
      // Also exclude device approval and passkey login endpoints (unauthenticated flow during login)
      const noRefreshEndpoints = ['/system/auth/login', '/system/auth/register', '/system/auth/refresh', '/system/auth/logout', '/system/auth/mfa', '/system/auth/approve-device', '/system/auth/deny-device', '/system/auth/forgot-password', '/system/auth/reset-password', '/system/auth/passkeys/login']
      const isNoRefreshEndpoint = noRefreshEndpoints.some(ep => originalRequest.url?.includes(ep))
      if (isNoRefreshEndpoint) {
        return Promise.reject(transformError(error))
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        })
          .then(() => apiClient(originalRequest))
          .catch((err) => Promise.reject(err))
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        // Call refresh endpoint - cookies are sent automatically
        await axios.post(
          `${API_BASE_URL}/system/auth/refresh`,
          {},
          { withCredentials: true }
        )

        processQueue(null)

        // Retry original request with new cookies
        return apiClient(originalRequest)
      } catch (refreshError) {
        processQueue(refreshError as Error)

        // Clear session flag so auth context knows we're logged out
        localStorage.removeItem('exoauth_has_session')

        // Check if it's a force re-auth error (e.g., password changed, account locked)
        const axiosRefreshError = refreshError as AxiosError<ApiResponse<unknown>>
        const errorCode = axiosRefreshError.response?.data?.errors?.[0]?.code
        const isForceReauth = errorCode === 'AUTH_FORCE_REAUTH'

        // Dispatch appropriate event so auth context can react
        window.dispatchEvent(
          new CustomEvent(isForceReauth ? 'auth:force-reauth' : 'auth:session-expired')
        )

        return Promise.reject(transformError(error))
      } finally {
        isRefreshing = false
      }
    }

    return Promise.reject(transformError(error))
  }
)

// Transform axios error to our ApiError format
function transformError(error: AxiosError<ApiResponse<unknown>>): ApiError {
  const response = error.response?.data

  if (response?.errors && response.errors.length > 0) {
    return {
      ...response.errors[0],
      data: response.data as Record<string, unknown> | undefined,
    }
  }

  return {
    code: response?.status === 'error' ? 'API_ERROR' : 'UNKNOWN_ERROR',
    message: response?.message || error.message || 'An unexpected error occurred',
  }
}

// Helper to extract data from API response
export function extractData<T>(response: { data: ApiResponse<T> }): T {
  return response.data.data
}

// Helper to check if response was successful
export function isSuccess<T>(response: ApiResponse<T>): boolean {
  return response.status === 'success'
}

export default apiClient
