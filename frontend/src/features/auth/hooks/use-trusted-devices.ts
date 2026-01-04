import { useQuery } from '@tanstack/react-query'
import { trustedDevicesApi } from '../api/trusted-devices-api'

export const TRUSTED_DEVICES_QUERY_KEY = ['auth', 'trusted-devices'] as const

export function useTrustedDevices() {
  return useQuery({
    queryKey: TRUSTED_DEVICES_QUERY_KEY,
    queryFn: trustedDevicesApi.getDevices,
  })
}
