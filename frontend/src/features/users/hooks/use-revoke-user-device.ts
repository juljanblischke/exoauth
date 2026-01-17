import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { userDevicesApi } from '../api/user-devices-api'
import { USER_DEVICES_KEY } from './use-user-devices'
import { SYSTEM_USERS_KEY } from './use-system-users'
import type { DeviceDto } from '@/features/auth/types/device'

interface RevokeUserDeviceParams {
  userId: string
  deviceId: string
}

export function useRevokeUserDevice() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: async ({ userId, deviceId }: RevokeUserDeviceParams) => {
      return userDevicesApi.revokeDevice(userId, deviceId)
    },
    onSuccess: (_, { userId, deviceId }) => {
      // Update the device status in cache
      queryClient.setQueryData<DeviceDto[]>(
        [...USER_DEVICES_KEY, userId],
        (old) => {
          if (!old) return old
          return old.map((device) =>
            device.id === deviceId
              ? { ...device, status: 'Revoked' as const, revokedAt: new Date().toISOString() }
              : device
          )
        }
      )
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      toast.success(t('users:devices.revokeSuccess'))
    },
  })
}
