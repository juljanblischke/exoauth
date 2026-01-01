import { useQuery } from '@tanstack/react-query'
import { deviceApprovalApi } from '../api/device-approval-api'

const APPROVE_DEVICE_KEY = ['auth', 'approve-device'] as const

export function useApproveDeviceByLink(token: string) {
  return useQuery({
    queryKey: [...APPROVE_DEVICE_KEY, token],
    queryFn: () => deviceApprovalApi.approveByLink(token),
    enabled: !!token,
    retry: false,
    staleTime: Infinity,
  })
}
