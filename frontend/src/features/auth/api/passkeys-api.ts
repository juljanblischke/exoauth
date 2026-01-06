import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type { AuthResponse } from '@/types/auth'
import type {
  PasskeyDto,
  GetPasskeysResponse,
  PasskeyRegisterOptionsResponse,
  PasskeyRegisterRequest,
  PasskeyLoginOptionsResponse,
  PasskeyLoginRequest,
  RenamePasskeyRequest,
} from '../types/passkey'

export const passkeysApi = {
  /**
   * Get all passkeys for the current user
   */
  getAll: async (): Promise<PasskeyDto[]> => {
    const response = await apiClient.get<ApiResponse<GetPasskeysResponse>>(
      '/auth/passkeys'
    )
    return extractData(response).passkeys
  },

  /**
   * Get WebAuthn registration options
   */
  getRegisterOptions: async (): Promise<PasskeyRegisterOptionsResponse> => {
    const response = await apiClient.post<ApiResponse<PasskeyRegisterOptionsResponse>>(
      '/auth/passkeys/register/options'
    )
    return extractData(response)
  },

  /**
   * Complete passkey registration
   */
  register: async (data: PasskeyRegisterRequest): Promise<PasskeyDto> => {
    const response = await apiClient.post<ApiResponse<PasskeyDto>>(
      '/auth/passkeys/register',
      data
    )
    return extractData(response)
  },

  /**
   * Get WebAuthn login options
   */
  getLoginOptions: async (): Promise<PasskeyLoginOptionsResponse> => {
    const response = await apiClient.post<ApiResponse<PasskeyLoginOptionsResponse>>(
      '/auth/passkeys/login/options'
    )
    return extractData(response)
  },

  /**
   * Complete passkey login
   */
  login: async (data: PasskeyLoginRequest): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      '/auth/passkeys/login',
      data
    )
    return extractData(response)
  },

  /**
   * Rename a passkey
   */
  rename: async (id: string, data: RenamePasskeyRequest): Promise<PasskeyDto> => {
    const response = await apiClient.patch<ApiResponse<PasskeyDto>>(
      `/auth/passkeys/${id}`,
      data
    )
    return extractData(response)
  },

  /**
   * Delete a passkey
   */
  delete: async (id: string): Promise<void> => {
    await apiClient.delete<ApiResponse<void>>(`/auth/passkeys/${id}`)
  },
}
