import { useQuery } from '@tanstack/react-query'
import { userDevicesApi } from '../api/user-devices-api'

export const USER_DEVICES_KEY = ['user-devices'] as const

export function useUserDevices(userId: string | undefined) {
  return useQuery({
    queryKey: [...USER_DEVICES_KEY, userId],
    queryFn: () => userDevicesApi.getDevices(userId!),
    enabled: !!userId,
  })
}
