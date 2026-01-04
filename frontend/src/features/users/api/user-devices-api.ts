import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  TrustedDeviceDto,
  RemoveDeviceResponse,
  RemoveAllDevicesResponse,
} from '@/features/auth/types/trusted-device'

export const userDevicesApi = {
  /**
   * Get user's trusted devices (admin view)
   */
  getDevices: async (userId: string): Promise<TrustedDeviceDto[]> => {
    const { data } = await apiClient.get<ApiResponse<TrustedDeviceDto[]>>(
      `/system/users/${userId}/devices`
    )
    return data.data
  },

  /**
   * Remove a specific trusted device for a user
   */
  removeDevice: async (
    userId: string,
    deviceId: string
  ): Promise<RemoveDeviceResponse> => {
    const { data } = await apiClient.delete<ApiResponse<RemoveDeviceResponse>>(
      `/system/users/${userId}/devices/${deviceId}`
    )
    return data.data
  },

  /**
   * Remove all trusted devices for a user
   */
  removeAllDevices: async (userId: string): Promise<RemoveAllDevicesResponse> => {
    const { data } = await apiClient.delete<ApiResponse<RemoveAllDevicesResponse>>(
      `/system/users/${userId}/devices`
    )
    return data.data
  },
}
