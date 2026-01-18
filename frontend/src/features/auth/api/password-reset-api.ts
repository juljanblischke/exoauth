import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type {
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
} from '@/types/auth'

export const passwordResetApi = {
  /**
   * Request a password reset email
   */
  forgotPassword: async (
    request: ForgotPasswordRequest
  ): Promise<ForgotPasswordResponse> => {
    const response = await apiClient.post<ApiResponse<ForgotPasswordResponse>>(
      '/system/auth/forgot-password',
      request
    )
    return extractData(response)
  },

  /**
   * Reset password with token from email
   */
  resetPassword: async (
    request: ResetPasswordRequest
  ): Promise<ResetPasswordResponse> => {
    const response = await apiClient.post<ApiResponse<ResetPasswordResponse>>(
      '/system/auth/reset-password',
      request
    )
    return extractData(response)
  },
}
