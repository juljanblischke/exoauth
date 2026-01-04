import { useQuery } from '@tanstack/react-query'
import { userDevicesApi } from '../api/user-devices-api'

export const USER_TRUSTED_DEVICES_KEY = ['user-trusted-devices'] as const

export function useUserTrustedDevices(userId: string | undefined) {
  return useQuery({
    queryKey: [...USER_TRUSTED_DEVICES_KEY, userId],
    queryFn: () => userDevicesApi.getDevices(userId!),
    enabled: !!userId,
  })
}
