import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  Loader2,
  Trash2,
  X,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared'
import { ConfirmDialog } from '@/components/shared/feedback'
import {
  useUserTrustedDevices,
  useRemoveUserTrustedDevice,
  useRemoveAllUserTrustedDevices,
} from '../hooks'
import type { TrustedDeviceDto } from '@/features/auth/types/trusted-device'

interface UserDevicesSectionProps {
  userId: string
  onDevicesRemoved?: () => void
}

function DeviceIcon({
  deviceType,
  className,
}: {
  deviceType: string | null
  className?: string
}) {
  switch (deviceType?.toLowerCase()) {
    case 'mobile':
      return <Smartphone className={className} />
    case 'tablet':
      return <Tablet className={className} />
    default:
      return <Monitor className={className} />
  }
}

interface DeviceItemProps {
  device: TrustedDeviceDto
  onRemove: (deviceId: string) => void
  isRemoving: boolean
  removingDeviceId: string | null
}

function DeviceItem({
  device,
  onRemove,
  isRemoving,
  removingDeviceId,
}: DeviceItemProps) {
  const { t } = useTranslation()
  const isThisRemoving = isRemoving && removingDeviceId === device.id

  const browserInfo = [device.browser, device.browserVersion]
    .filter(Boolean)
    .join(' ')
  const osInfo = [device.operatingSystem, device.osVersion]
    .filter(Boolean)
    .join(' ')
  const deviceInfo = [browserInfo, osInfo].filter(Boolean).join(' · ')

  return (
    <div className="flex items-start gap-3 p-3 rounded-lg border bg-card">
      <div className="flex-shrink-0 p-2 rounded-full bg-muted">
        <DeviceIcon
          deviceType={device.deviceType}
          className="h-4 w-4 text-muted-foreground"
        />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium text-sm truncate">
            {device.name || t('auth:trustedDevices.unknownDevice')}
          </span>
        </div>

        <div className="mt-1 text-xs text-muted-foreground space-y-0.5">
          {deviceInfo && <div>{deviceInfo}</div>}
          {device.locationDisplay && (
            <div className="flex items-center gap-1.5">
              <Globe className="h-3 w-3" />
              <span>{device.locationDisplay}</span>
            </div>
          )}
          {device.lastUsedAt && (
            <div className="flex items-center gap-1.5">
              <span>{t('auth:trustedDevices.lastUsed')}:</span>
              <RelativeTime date={device.lastUsedAt} />
            </div>
          )}
        </div>
      </div>

      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-muted-foreground hover:text-destructive"
            onClick={() => onRemove(device.id)}
            disabled={isRemoving}
          >
            {isThisRemoving ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <X className="h-4 w-4" />
            )}
          </Button>
        </TooltipTrigger>
        <TooltipContent>{t('users:trustedDevices.remove')}</TooltipContent>
      </Tooltip>
    </div>
  )
}

export function UserDevicesSection({
  userId,
  onDevicesRemoved,
}: UserDevicesSectionProps) {
  const { t } = useTranslation()
  const [showRemoveAllDialog, setShowRemoveAllDialog] = useState(false)
  const [removingDeviceId, setRemovingDeviceId] = useState<string | null>(null)

  const { data: devices, isLoading } = useUserTrustedDevices(userId)
  const { mutate: removeDevice, isPending: isRemovingSingle } =
    useRemoveUserTrustedDevice()
  const { mutate: removeAllDevices, isPending: isRemovingAll } =
    useRemoveAllUserTrustedDevices()

  const handleRemoveSingle = (deviceId: string) => {
    setRemovingDeviceId(deviceId)
    removeDevice(
      { userId, deviceId },
      {
        onSettled: () => {
          setRemovingDeviceId(null)
        },
      }
    )
  }

  const handleRemoveAll = () => {
    removeAllDevices(userId, {
      onSuccess: () => {
        setShowRemoveAllDialog(false)
        onDevicesRemoved?.()
      },
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Skeleton className="h-5 w-32" />
          <Skeleton className="h-8 w-24" />
        </div>
        <div className="space-y-2">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      </div>
    )
  }

  const deviceCount = devices?.length ?? 0

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-medium">
          {t('users:trustedDevices.title')} ({deviceCount})
        </h4>
        {deviceCount > 0 && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowRemoveAllDialog(true)}
            disabled={isRemovingAll}
          >
            {isRemovingAll ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <Trash2 className="h-4 w-4 mr-2" />
            )}
            {t('users:trustedDevices.removeAll')}
          </Button>
        )}
      </div>

      {deviceCount === 0 ? (
        <p className="text-sm text-muted-foreground py-4 text-center">
          {t('users:trustedDevices.noDevices')}
        </p>
      ) : (
        <div className="space-y-2 max-h-64 overflow-y-auto">
          {devices?.map((device) => (
            <DeviceItem
              key={device.id}
              device={device}
              onRemove={handleRemoveSingle}
              isRemoving={isRemovingSingle}
              removingDeviceId={removingDeviceId}
            />
          ))}
        </div>
      )}

      <ConfirmDialog
        open={showRemoveAllDialog}
        onOpenChange={setShowRemoveAllDialog}
        title={t('users:trustedDevices.removeAll')}
        description={t('users:trustedDevices.removeAllConfirm')}
        confirmLabel={t('users:trustedDevices.removeAll')}
        variant="destructive"
        onConfirm={handleRemoveAll}
        isLoading={isRemovingAll}
      />
    </div>
  )
}
