import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { trustedDevicesApi } from '../api/trusted-devices-api'
import { TRUSTED_DEVICES_QUERY_KEY } from './use-trusted-devices'
import type { TrustedDeviceDto, RenameDeviceRequest } from '../types/trusted-device'

interface RenameDeviceParams {
  deviceId: string
  request: RenameDeviceRequest
}

export function useRenameTrustedDevice() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: ({ deviceId, request }: RenameDeviceParams) =>
      trustedDevicesApi.renameDevice(deviceId, request),
    onSuccess: (updatedDevice) => {
      // Update the device in cache
      queryClient.setQueryData<TrustedDeviceDto[]>(
        TRUSTED_DEVICES_QUERY_KEY,
        (old) => {
          if (!old) return old
          return old.map((device) =>
            device.id === updatedDevice.id ? updatedDevice : device
          )
        }
      )
      toast.success(t('auth:trustedDevices.renameSuccess'))
    },
  })
}
