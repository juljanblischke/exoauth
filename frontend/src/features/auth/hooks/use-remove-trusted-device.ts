import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { trustedDevicesApi } from '../api/trusted-devices-api'
import { TRUSTED_DEVICES_QUERY_KEY } from './use-trusted-devices'
import type { TrustedDeviceDto } from '../types/trusted-device'

export function useRemoveTrustedDevice() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: trustedDevicesApi.removeDevice,
    onSuccess: (_, deviceId) => {
      // Remove the device from cache
      queryClient.setQueryData<TrustedDeviceDto[]>(
        TRUSTED_DEVICES_QUERY_KEY,
        (old) => {
          if (!old) return old
          return old.filter((device) => device.id !== deviceId)
        }
      )
      toast.success(t('auth:trustedDevices.removeSuccess'))
    },
  })
}
