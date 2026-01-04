import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { userDevicesApi } from '../api/user-devices-api'
import { USER_TRUSTED_DEVICES_KEY } from './use-user-trusted-devices'
import { SYSTEM_USERS_KEY } from './use-system-users'

interface RemoveUserTrustedDeviceParams {
  userId: string
  deviceId: string
}

export function useRemoveUserTrustedDevice() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: async ({ userId, deviceId }: RemoveUserTrustedDeviceParams) => {
      return userDevicesApi.removeDevice(userId, deviceId)
    },
    onSuccess: (_, { userId }) => {
      queryClient.invalidateQueries({
        queryKey: [...USER_TRUSTED_DEVICES_KEY, userId],
      })
      queryClient.invalidateQueries({ queryKey: SYSTEM_USERS_KEY })
      toast.success(t('users:trustedDevices.removeSuccess'))
    },
  })
}
