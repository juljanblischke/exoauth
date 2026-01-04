import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  MoreVertical,
  Trash2,
  Pencil,
  Loader2,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { RelativeTime } from '@/components/shared'
import type { TrustedDeviceDto } from '../types/trusted-device'

interface TrustedDeviceCardProps {
  device: TrustedDeviceDto
  onRename?: (device: TrustedDeviceDto) => void
  onRemove: (deviceId: string) => void
  isRemoving?: boolean
  showRename?: boolean
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

export function TrustedDeviceCard({
  device,
  onRename,
  onRemove,
  isRemoving,
  showRename = true,
}: TrustedDeviceCardProps) {
  const { t } = useTranslation()

  const browserInfo = [device.browser, device.browserVersion]
    .filter(Boolean)
    .join(' ')
  const osInfo = [device.operatingSystem, device.osVersion]
    .filter(Boolean)
    .join(' ')
  const deviceInfo = [browserInfo, osInfo].filter(Boolean).join(' · ')

  return (
    <div className="flex items-start gap-4 p-4 rounded-lg border bg-card">
      <div className="flex-shrink-0 p-2 rounded-full bg-muted">
        <DeviceIcon
          deviceType={device.deviceType}
          className="h-5 w-5 text-muted-foreground"
        />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <h4 className="font-medium truncate">
            {device.name || t('auth:trustedDevices.unknownDevice')}
          </h4>
          {device.isCurrent && (
            <Badge variant="default" className="text-xs">
              {t('auth:trustedDevices.thisDevice')}
            </Badge>
          )}
        </div>

        <div className="mt-1 text-sm text-muted-foreground space-y-0.5">
          {deviceInfo && <div>{deviceInfo}</div>}
          {device.locationDisplay && (
            <div className="flex items-center gap-1.5">
              <Globe className="h-3.5 w-3.5" />
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

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="icon" disabled={isRemoving}>
            {isRemoving ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <MoreVertical className="h-4 w-4" />
            )}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {showRename && onRename && (
            <DropdownMenuItem onClick={() => onRename(device)}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('auth:trustedDevices.rename')}
            </DropdownMenuItem>
          )}
          {!device.isCurrent && (
            <DropdownMenuItem
              onClick={() => onRemove(device.id)}
              className="text-destructive focus:text-destructive"
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('auth:trustedDevices.remove')}
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
