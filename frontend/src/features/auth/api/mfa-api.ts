import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type { AuthResponse, MfaVerifyRequest } from '@/types/auth'
import type {
  MfaSetupResponse,
  MfaConfirmRequest,
  MfaConfirmResponse,
  MfaDisableRequest,
  MfaDisableResponse,
  RegenerateBackupCodesRequest,
  RegenerateBackupCodesResponse,
} from '../types'

export const mfaApi = {
  /**
   * Start MFA setup - returns QR code and secret
   * @param setupToken - Optional setup token for forced MFA flow (login/register)
   */
  setup: async (setupToken?: string): Promise<MfaSetupResponse> => {
    const body = setupToken ? { setupToken } : undefined
    const response = await apiClient.post<ApiResponse<MfaSetupResponse>>(
      '/system/auth/mfa/setup',
      body
    )
    return extractData(response)
  },

  /**
   * Confirm MFA setup with TOTP code - returns backup codes
   * When using setupToken, also returns full auth response (user, tokens)
   * @param request - The confirmation request with TOTP code
   * @param setupToken - Optional setup token for forced MFA flow (login/register)
   */
  confirm: async (
    request: MfaConfirmRequest,
    setupToken?: string
  ): Promise<MfaConfirmResponse> => {
    const body = setupToken ? { ...request, setupToken } : request
    const response = await apiClient.post<ApiResponse<MfaConfirmResponse>>(
      '/system/auth/mfa/confirm',
      body
    )
    return extractData(response)
  },

  /**
   * Verify MFA code during login
   */
  verify: async (request: MfaVerifyRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/system/auth/mfa/verify',
      request
    )
    return extractData(response)
  },

  /**
   * Disable MFA for the current user
   */
  disable: async (request: MfaDisableRequest): Promise<MfaDisableResponse> => {
    const response = await apiClient.post<ApiResponse<MfaDisableResponse>>(
      '/system/auth/mfa/disable',
      request
    )
    return extractData(response)
  },

  /**
   * Regenerate backup codes (requires current TOTP code)
   */
  regenerateBackupCodes: async (
    request: RegenerateBackupCodesRequest
  ): Promise<RegenerateBackupCodesResponse> => {
    const response = await apiClient.post<ApiResponse<RegenerateBackupCodesResponse>>(
      '/system/auth/mfa/backup-codes',
      request
    )
    return extractData(response)
  },
}
