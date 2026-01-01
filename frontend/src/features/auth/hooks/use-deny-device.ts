import { useMutation } from '@tanstack/react-query'
import { deviceApprovalApi } from '../api/device-approval-api'
import type { DenyDeviceRequest } from '../types'

export function useDenyDevice() {
  return useMutation({
    mutationFn: (data: DenyDeviceRequest) => deviceApprovalApi.denyDevice(data),
  })
}
