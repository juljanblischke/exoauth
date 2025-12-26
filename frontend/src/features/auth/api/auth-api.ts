import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type {
  User,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  AcceptInviteRequest,
  LogoutResponse,
} from '../types'

export const authApi = {
  /**
   * Login with email and password
   */
  login: async (request: LoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/auth/login',
      request
    )
    return extractData(response)
  },

  /**
   * Register a new user (first user only when registration is open)
   */
  register: async (request: RegisterRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/auth/register',
      request
    )
    return extractData(response)
  },

  /**
   * Accept an invitation and set password
   */
  acceptInvite: async (request: AcceptInviteRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/auth/accept-invite',
      request
    )
    return extractData(response)
  },

  /**
   * Logout the current user
   */
  logout: async (): Promise<LogoutResponse> => {
    const response = await apiClient.post<ApiResponse<LogoutResponse>>(
      '/auth/logout'
    )
    return extractData(response)
  },

  /**
   * Get current authenticated user
   */
  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<ApiResponse<User>>('/auth/me')
    return extractData(response)
  },

  /**
   * Refresh the access token
   * Note: Usually handled automatically by axios interceptor
   */
  refresh: async (): Promise<void> => {
    await apiClient.post('/auth/refresh')
  },
}
