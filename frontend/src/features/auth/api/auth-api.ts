import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type {
  User,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  AcceptInviteRequest,
  LogoutResponse,
  InviteValidationDto,
  RequestMagicLinkRequest,
  RequestMagicLinkResponse,
} from '../types'

export const authApi = {
  /**
   * Login with email and password
   */
  login: async (request: LoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/system/auth/login',
      request
    )
    return extractData(response)
  },

  /**
   * Register a new user (first user only when registration is open)
   */
  register: async (request: RegisterRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/system/auth/register',
      request
    )
    return extractData(response)
  },

  /**
   * Accept an invitation and set password
   */
  acceptInvite: async (request: AcceptInviteRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/system/auth/accept-invite',
      request
    )
    return extractData(response)
  },

  /**
   * Logout the current user
   */
  logout: async (): Promise<LogoutResponse> => {
    const response = await apiClient.post<ApiResponse<LogoutResponse>>(
      '/system/auth/logout'
    )
    return extractData(response)
  },

  /**
   * Get current authenticated user
   */
  getCurrentUser: async (): Promise<User> => {
    const response = await apiClient.get<ApiResponse<User>>('/system/auth/me')
    return extractData(response)
  },

  /**
   * Refresh the access token
   * Note: Usually handled automatically by axios interceptor
   */
  refresh: async (): Promise<void> => {
    await apiClient.post('/system/auth/refresh')
  },

  /**
   * Validate an invitation token and get details (public endpoint)
   */
  validateInvite: async (token: string): Promise<InviteValidationDto> => {
    const response = await apiClient.get<ApiResponse<InviteValidationDto>>(
      '/system/auth/invite',
      { params: { token } }
    )
    return extractData(response)
  },

  /**
   * Request a magic link for passwordless login
   */
  requestMagicLink: async (
    request: RequestMagicLinkRequest
  ): Promise<RequestMagicLinkResponse> => {
    const response = await apiClient.post<ApiResponse<RequestMagicLinkResponse>>(
      '/system/auth/magic-link/request',
      request
    )
    return extractData(response)
  },
}
