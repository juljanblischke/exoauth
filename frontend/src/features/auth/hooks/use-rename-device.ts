import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { devicesApi } from '../api/devices-api'
import { DEVICES_QUERY_KEY } from './use-devices'
import type { DeviceDto, RenameDeviceRequest } from '../types/device'

interface RenameDeviceParams {
  deviceId: string
  request: RenameDeviceRequest
}

export function useRenameDevice() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: ({ deviceId, request }: RenameDeviceParams) =>
      devicesApi.renameDevice(deviceId, request),
    onSuccess: (updatedDevice) => {
      // Update the device in cache
      queryClient.setQueryData<DeviceDto[]>(DEVICES_QUERY_KEY, (old) => {
        if (!old) return old
        return old.map((device) =>
          device.id === updatedDevice.id ? updatedDevice : device
        )
      })
      toast.success(t('auth:devices.rename.success'))
    },
  })
}
