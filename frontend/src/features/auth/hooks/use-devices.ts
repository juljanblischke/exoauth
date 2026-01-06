import { useQuery } from '@tanstack/react-query'
import { devicesApi } from '../api/devices-api'

export const DEVICES_QUERY_KEY = ['auth', 'devices'] as const

export function useDevices() {
  return useQuery({
    queryKey: DEVICES_QUERY_KEY,
    queryFn: devicesApi.getDevices,
  })
}
