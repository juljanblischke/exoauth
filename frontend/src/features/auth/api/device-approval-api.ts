import apiClient, { extractData } from '@/lib/axios'
import type { ApiResponse } from '@/types/api'
import type {
  ApproveDeviceByCodeRequest,
  ApproveDeviceByCodeResponse,
  ApproveDeviceByLinkResponse,
  DenyDeviceRequest,
  DenyDeviceResponse,
} from '../types'

export const deviceApprovalApi = {
  /**
   * Approve device by entering code from email
   */
  approveByCode: async (
    request: ApproveDeviceByCodeRequest
  ): Promise<ApproveDeviceByCodeResponse> => {
    const response = await apiClient.post<ApiResponse<ApproveDeviceByCodeResponse>>(
      '/system/auth/approve-device',
      request
    )
    return extractData(response)
  },

  /**
   * Approve device by clicking email link
   */
  approveByLink: async (token: string): Promise<ApproveDeviceByLinkResponse> => {
    const response = await apiClient.get<ApiResponse<ApproveDeviceByLinkResponse>>(
      '/system/auth/approve-device-link',
      { params: { token } }
    )
    return extractData(response)
  },

  /**
   * Deny a device (marks as suspicious)
   */
  denyDevice: async (request: DenyDeviceRequest): Promise<DenyDeviceResponse> => {
    const response = await apiClient.post<ApiResponse<DenyDeviceResponse>>(
      '/system/auth/deny-device',
      request
    )
    return extractData(response)
  },
}
