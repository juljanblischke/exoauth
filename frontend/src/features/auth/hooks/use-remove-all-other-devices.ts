import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { trustedDevicesApi } from '../api/trusted-devices-api'
import { TRUSTED_DEVICES_QUERY_KEY } from './use-trusted-devices'
import type { TrustedDeviceDto } from '../types/trusted-device'

export function useRemoveAllOtherDevices() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: async () => {
      // Get all devices except current
      const devices = queryClient.getQueryData<TrustedDeviceDto[]>(
        TRUSTED_DEVICES_QUERY_KEY
      )
      const otherDevices = devices?.filter((d) => !d.isCurrent) ?? []

      // Remove each device
      await Promise.all(
        otherDevices.map((device) => trustedDevicesApi.removeDevice(device.id))
      )

      return { removedCount: otherDevices.length }
    },
    onSuccess: () => {
      // Keep only the current device in cache
      queryClient.setQueryData<TrustedDeviceDto[]>(
        TRUSTED_DEVICES_QUERY_KEY,
        (old) => {
          if (!old) return old
          return old.filter((device) => device.isCurrent)
        }
      )
      toast.success(t('auth:trustedDevices.removeAllSuccess'))
    },
  })
}
