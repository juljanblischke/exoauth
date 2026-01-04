import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type {
  TrustedDeviceDto,
  RenameDeviceRequest,
  RemoveDeviceResponse,
} from '../types/trusted-device'

export const trustedDevicesApi = {
  /**
   * Get all trusted devices for the current user
   */
  getDevices: async (): Promise<TrustedDeviceDto[]> => {
    const response = await apiClient.get<ApiResponse<TrustedDeviceDto[]>>(
      '/auth/devices'
    )
    return extractData(response)
  },

  /**
   * Remove a trusted device
   */
  removeDevice: async (deviceId: string): Promise<RemoveDeviceResponse> => {
    const response = await apiClient.delete<ApiResponse<RemoveDeviceResponse>>(
      `/auth/devices/${deviceId}`
    )
    return extractData(response)
  },

  /**
   * Rename a trusted device
   */
  renameDevice: async (
    deviceId: string,
    request: RenameDeviceRequest
  ): Promise<TrustedDeviceDto> => {
    const response = await apiClient.put<ApiResponse<TrustedDeviceDto>>(
      `/auth/devices/${deviceId}/name`,
      request
    )
    return extractData(response)
  },
}
