import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type {
  DeviceSessionDto,
  UpdateSessionRequest,
  RevokeSessionResponse,
  RevokeAllSessionsResponse,
} from '../types'

export const sessionsApi = {
  /**
   * Get all active sessions for the current user
   */
  getSessions: async (): Promise<DeviceSessionDto[]> => {
    const response = await apiClient.get<ApiResponse<DeviceSessionDto[]>>(
      '/auth/sessions'
    )
    return extractData(response)
  },

  /**
   * Revoke a specific session
   */
  revokeSession: async (sessionId: string): Promise<RevokeSessionResponse> => {
    const response = await apiClient.delete<ApiResponse<RevokeSessionResponse>>(
      `/auth/sessions/${sessionId}`
    )
    return extractData(response)
  },

  /**
   * Revoke all sessions except the current one
   */
  revokeAllSessions: async (): Promise<RevokeAllSessionsResponse> => {
    const response = await apiClient.delete<ApiResponse<RevokeAllSessionsResponse>>(
      '/auth/sessions'
    )
    return extractData(response)
  },

  /**
   * Update session (rename)
   */
  updateSession: async (
    sessionId: string,
    request: UpdateSessionRequest
  ): Promise<DeviceSessionDto> => {
    const response = await apiClient.patch<ApiResponse<DeviceSessionDto>>(
      `/auth/sessions/${sessionId}`,
      request
    )
    return extractData(response)
  },
}
