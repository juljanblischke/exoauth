import { useMutation } from '@tanstack/react-query'
import { deviceApprovalApi } from '../api/device-approval-api'
import type { ApproveDeviceByCodeRequest } from '../types'

export function useApproveDeviceByCode() {
  return useMutation({
    mutationFn: (data: ApproveDeviceByCodeRequest) =>
      deviceApprovalApi.approveByCode(data),
  })
}
