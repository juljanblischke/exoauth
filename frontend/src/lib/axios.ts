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

    // Handle 401 Unauthorized - attempt token refresh
    if (error.response?.status === 401 && !originalRequest._retry) {
      // Don't try to refresh for login/register/refresh endpoints (would cause infinite loop)
      const noRefreshEndpoints = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/logout']
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
        console.log('[Auth] Attempting token refresh...')
        await axios.post(
          `${API_BASE_URL}/auth/refresh`,
          {},
          { withCredentials: true }
        )

        console.log('[Auth] Token refresh successful')
        processQueue(null)

        // Retry original request with new cookies
        return apiClient(originalRequest)
      } catch (refreshError) {
        console.error('[Auth] Token refresh failed:', refreshError)
        processQueue(refreshError as Error)

        // Clear session flag so auth context knows we're logged out
        localStorage.removeItem('exoauth_has_session')

        // Dispatch custom event so auth context can react
        window.dispatchEvent(new CustomEvent('auth:session-expired'))

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
    return response.errors[0]
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
