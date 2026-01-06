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
import { DeviceStatusBadge } from '@/features/auth/components/device-status-badge'
import { DeviceDetailsSheet } from '@/features/auth/components/device-details-sheet'
import {
  useUserDevices,
  useRevokeUserDevice,
  useRevokeAllUserDevices,
} from '../hooks'
import { normalizeDeviceStatus, type DeviceDto } from '@/features/auth/types/device'

interface UserDevicesSectionProps {
  userId: string
  onDevicesRevoked?: () => void
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
  device: DeviceDto
  onClick: (device: DeviceDto) => void
  onRevoke: (deviceId: string) => void
  isRevoking: boolean
  revokingDeviceId: string | null
}

function DeviceItem({
  device,
  onClick,
  onRevoke,
  isRevoking,
  revokingDeviceId,
}: DeviceItemProps) {
  const { t } = useTranslation()
  const isThisRevoking = isRevoking && revokingDeviceId === device.id

  const browserInfo = [device.browser, device.browserVersion]
    .filter(Boolean)
    .join(' ')
  const osInfo = [device.operatingSystem, device.osVersion]
    .filter(Boolean)
    .join(' ')
  const deviceInfo = [browserInfo, osInfo].filter(Boolean).join(' · ')

  const locationDisplay = device.locationDisplay || [device.city, device.country]
    .filter(Boolean)
    .join(', ')

  const normalizedStatus = normalizeDeviceStatus(device.status)
  const isRevoked = normalizedStatus === 'Revoked'

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={() => onClick(device)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault()
          onClick(device)
        }
      }}
      className="w-full text-left flex items-start gap-3 p-3 rounded-lg border bg-card hover:bg-accent/50 transition-colors cursor-pointer"
    >
      <div className="flex-shrink-0 p-2 rounded-full bg-muted">
        <DeviceIcon
          deviceType={device.deviceType}
          className="h-4 w-4 text-muted-foreground"
        />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium text-sm truncate">
            {device.name || device.displayName || t('auth:devices.unknownDevice')}
          </span>
          <DeviceStatusBadge status={device.status} />
        </div>

        <div className="mt-1 text-xs text-muted-foreground space-y-0.5">
          {deviceInfo && <div>{deviceInfo}</div>}
          {locationDisplay && (
            <div className="flex items-center gap-1.5">
              <Globe className="h-3 w-3" />
              <span>{locationDisplay}</span>
            </div>
          )}
          <div className="flex items-center gap-1.5">
            <span>{t('auth:devices.info.lastUsed')}:</span>
            <RelativeTime date={device.lastUsedAt} />
          </div>
        </div>
      </div>

      {!isRevoked && (
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-muted-foreground hover:text-destructive"
              onClick={(e) => {
                e.stopPropagation()
                onRevoke(device.id)
              }}
              disabled={isRevoking}
            >
              {isThisRevoking ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <X className="h-4 w-4" />
              )}
            </Button>
          </TooltipTrigger>
          <TooltipContent>{t('auth:devices.actions.revoke')}</TooltipContent>
        </Tooltip>
      )}
    </div>
  )
}

export function UserDevicesSection({
  userId,
  onDevicesRevoked,
}: UserDevicesSectionProps) {
  const { t } = useTranslation()
  const [showRevokeAllDialog, setShowRevokeAllDialog] = useState(false)
  const [revokingDeviceId, setRevokingDeviceId] = useState<string | null>(null)
  const [selectedDevice, setSelectedDevice] = useState<DeviceDto | null>(null)

  const { data: devices, isLoading } = useUserDevices(userId)
  const { mutate: revokeDevice, isPending: isRevokingSingle } =
    useRevokeUserDevice()
  const { mutate: revokeAllDevices, isPending: isRevokingAll } =
    useRevokeAllUserDevices()

  const handleRevokeSingle = (deviceId: string) => {
    setRevokingDeviceId(deviceId)
    revokeDevice(
      { userId, deviceId },
      {
        onSettled: () => {
          setRevokingDeviceId(null)
        },
      }
    )
  }

  const handleRevokeAll = () => {
    revokeAllDevices(userId, {
      onSuccess: () => {
        setShowRevokeAllDialog(false)
        onDevicesRevoked?.()
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

  // Filter out revoked devices for count and actions
  const activeDevices = devices?.filter((d) => normalizeDeviceStatus(d.status) !== 'Revoked') ?? []
  const deviceCount = devices?.length ?? 0
  const activeCount = activeDevices.length

  // Sort: trusted first, then pending, then revoked
  const sortedDevices = [...(devices ?? [])].sort((a, b) => {
    const statusOrder = { Trusted: 0, PendingApproval: 1, Revoked: 2 }
    const aStatus = normalizeDeviceStatus(a.status)
    const bStatus = normalizeDeviceStatus(b.status)
    return statusOrder[aStatus] - statusOrder[bStatus]
  })

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="text-sm font-medium">
          {t('auth:devices.title')} ({deviceCount})
        </h4>
        {activeCount > 0 && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowRevokeAllDialog(true)}
            disabled={isRevokingAll}
          >
            {isRevokingAll ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <Trash2 className="h-4 w-4 mr-2" />
            )}
            {t('auth:devices.actions.revokeAll')}
          </Button>
        )}
      </div>

      {deviceCount === 0 ? (
        <p className="text-sm text-muted-foreground py-4 text-center">
          {t('auth:devices.empty.title')}
        </p>
      ) : (
        <div className="space-y-2 max-h-64 overflow-y-auto">
          {sortedDevices.map((device) => (
            <DeviceItem
              key={device.id}
              device={device}
              onClick={setSelectedDevice}
              onRevoke={handleRevokeSingle}
              isRevoking={isRevokingSingle}
              revokingDeviceId={revokingDeviceId}
            />
          ))}
        </div>
      )}

      <ConfirmDialog
        open={showRevokeAllDialog}
        onOpenChange={setShowRevokeAllDialog}
        title={t('users:devices.revokeAllTitle')}
        description={t('users:devices.revokeAllDescription')}
        confirmLabel={t('auth:devices.actions.revokeAll')}
        variant="destructive"
        onConfirm={handleRevokeAll}
        isLoading={isRevokingAll}
      />

      <DeviceDetailsSheet
        device={selectedDevice}
        open={!!selectedDevice}
        onOpenChange={(open) => !open && setSelectedDevice(null)}
        onRevoke={(device) => {
          setSelectedDevice(null)
          handleRevokeSingle(device.id)
        }}
      />
    </div>
  )
}