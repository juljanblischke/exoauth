import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { devicesApi } from '../api/devices-api'
import { DEVICES_QUERY_KEY } from './use-devices'
import type { DeviceDto } from '../types/device'

export function useApproveDeviceFromSession() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: devicesApi.approveDeviceFromSession,
    onSuccess: (_, deviceId) => {
      // Update the device status in cache
      queryClient.setQueryData<DeviceDto[]>(DEVICES_QUERY_KEY, (old) => {
        if (!old) return old
        return old.map((device) =>
          device.id === deviceId
            ? { ...device, status: 'Trusted' as const, trustedAt: new Date().toISOString() }
            : device
        )
      })
      toast.success(t('auth:devices.approve.success'))
    },
  })
}
