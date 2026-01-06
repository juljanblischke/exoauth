import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type { AuthResponse } from '@/types/auth'
import type {
  DeviceDto,
  RenameDeviceRequest,
  RevokeDeviceResponse,
  RevokeAllDevicesResponse,
} from '../types/device'

export const devicesApi = {
  /**
   * Get all devices for the current user
   */
  getDevices: async (): Promise<DeviceDto[]> => {
    const response = await apiClient.get<ApiResponse<DeviceDto[]>>(
      '/auth/devices'
    )
    return extractData(response)
  },

  /**
   * Revoke a device (logout + remove trust)
   */
  revokeDevice: async (deviceId: string): Promise<RevokeDeviceResponse> => {
    const response = await apiClient.delete<ApiResponse<RevokeDeviceResponse>>(
      `/auth/devices/${deviceId}`
    )
    return extractData(response)
  },

  /**
   * Revoke all devices except the current one
   */
  revokeAllDevices: async (): Promise<RevokeAllDevicesResponse> => {
    const response = await apiClient.delete<ApiResponse<RevokeAllDevicesResponse>>(
      '/auth/devices'
    )
    return extractData(response)
  },

  /**
   * Rename a device
   */
  renameDevice: async (
    deviceId: string,
    request: RenameDeviceRequest
  ): Promise<DeviceDto> => {
    const response = await apiClient.put<ApiResponse<DeviceDto>>(
      `/auth/devices/${deviceId}/name`,
      request
    )
    return extractData(response)
  },

  /**
   * Approve a pending device from current session
   */
  approveDeviceFromSession: async (deviceId: string): Promise<AuthResponse> => {
    const response = await apiClient.post<ApiResponse<AuthResponse>>(
      `/auth/devices/${deviceId}/approve`
    )
    return extractData(response)
  },
}
