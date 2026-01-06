import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  DeviceDto,
  RevokeDeviceResponse,
  RevokeAllDevicesResponse,
} from '@/features/auth/types/device'

export const userDevicesApi = {
  /**
   * Get user's devices (admin view)
   */
  getDevices: async (userId: string): Promise<DeviceDto[]> => {
    const { data } = await apiClient.get<ApiResponse<DeviceDto[]>>(
      `/system/users/${userId}/devices`
    )
    return data.data
  },

  /**
   * Revoke a specific device for a user
   */
  revokeDevice: async (
    userId: string,
    deviceId: string
  ): Promise<RevokeDeviceResponse> => {
    const { data } = await apiClient.delete<ApiResponse<RevokeDeviceResponse>>(
      `/system/users/${userId}/devices/${deviceId}`
    )
    return data.data
  },

  /**
   * Revoke all devices for a user
   */
  revokeAllDevices: async (userId: string): Promise<RevokeAllDevicesResponse> => {
    const { data } = await apiClient.delete<ApiResponse<RevokeAllDevicesResponse>>(
      `/system/users/${userId}/devices`
    )
    return data.data
  },
}
