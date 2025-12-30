import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { DeviceSessionDto, RevokeAllSessionsResponse } from '@/features/auth/types'

// Admin action request types
export interface AdminActionRequest {
  reason?: string
}

// Admin action response types
export interface AdminActionResponse {
  success: boolean
}

export const userAdminApi = {
  // Reset user's MFA
  resetMfa: async (userId: string, request?: AdminActionRequest): Promise<AdminActionResponse> => {
    const { data } = await apiClient.post<ApiResponse<AdminActionResponse>>(
      `/system/users/${userId}/mfa/reset`,
      request || {}
    )
    return data.data
  },

  // Unlock locked user account
  unlock: async (userId: string, request?: AdminActionRequest): Promise<AdminActionResponse> => {
    const { data } = await apiClient.post<ApiResponse<AdminActionResponse>>(
      `/system/users/${userId}/unlock`,
      request || {}
    )
    return data.data
  },

  // Get user's sessions (admin view)
  getSessions: async (userId: string): Promise<DeviceSessionDto[]> => {
    const { data } = await apiClient.get<ApiResponse<DeviceSessionDto[]>>(
      `/system/users/${userId}/sessions`
    )
    return data.data
  },

  // Revoke all user sessions
  revokeSessions: async (userId: string): Promise<RevokeAllSessionsResponse> => {
    const { data } = await apiClient.delete<ApiResponse<RevokeAllSessionsResponse>>(
      `/system/users/${userId}/sessions`
    )
    return data.data
  },

  // Deactivate user
  deactivate: async (userId: string): Promise<void> => {
    await apiClient.post(`/system/users/${userId}/deactivate`)
  },

  // Activate user
  activate: async (userId: string): Promise<void> => {
    await apiClient.post(`/system/users/${userId}/activate`)
  },

  // Anonymize user (GDPR)
  anonymize: async (userId: string): Promise<AdminActionResponse> => {
    const { data } = await apiClient.post<ApiResponse<AdminActionResponse>>(
      `/system/users/${userId}/anonymize`
    )
    return data.data
  },
}
