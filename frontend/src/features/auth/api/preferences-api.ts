import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'

export interface UpdatePreferencesRequest {
  language: string
}

export interface UpdatePreferencesResponse {
  success: boolean
}

export const preferencesApi = {
  /**
   * Update user preferences (language, etc.)
   */
  updatePreferences: async (
    request: UpdatePreferencesRequest
  ): Promise<UpdatePreferencesResponse> => {
    const response = await apiClient.patch<ApiResponse<UpdatePreferencesResponse>>(
      '/system/auth/me/preferences',
      request
    )
    return extractData(response)
  },
}
