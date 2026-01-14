import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  MoreVertical,
  Trash2,
  Pencil,
  CheckCircle,
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
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { RelativeTime } from '@/components/shared'
import { DeviceStatusBadge } from './device-status-badge'
import { normalizeDeviceStatus, type DeviceDto } from '../types/device'

interface DeviceCardProps {
  device: DeviceDto
  onClick?: (device: DeviceDto) => void
  onRename?: (device: DeviceDto) => void
  onRevoke: (deviceId: string) => void
  onApprove?: (deviceId: string) => void
  isRevoking?: boolean
  isApproving?: boolean
  showRename?: boolean
  showApprove?: boolean
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

export function DeviceCard({
  device,
  onClick,
  onRename,
  onRevoke,
  onApprove,
  isRevoking,
  isApproving,
  showRename = true,
  showApprove = true,
}: DeviceCardProps) {
  const { t } = useTranslation()

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

  const isLoading = isRevoking || isApproving
  const normalizedStatus = normalizeDeviceStatus(device.status)
  const isPending = normalizedStatus === 'PendingApproval'
  const isRevoked = normalizedStatus === 'Revoked'

  const handleCardClick = () => {
    onClick?.(device)
  }

  return (
    <div className="flex items-start gap-4 p-4 rounded-lg border bg-card">
      <button
        type="button"
        className="flex items-start gap-4 flex-1 min-w-0 text-left hover:opacity-80 transition-opacity"
        onClick={handleCardClick}
      >
        <div className="flex-shrink-0 p-2 rounded-full bg-muted">
          <DeviceIcon
            deviceType={device.deviceType}
            className="h-5 w-5 text-muted-foreground"
          />
        </div>

        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <Tooltip>
              <TooltipTrigger asChild>
                <h4 className="font-medium truncate max-w-[200px] cursor-default">
                  {device.name || device.displayName || t('auth:devices.unknownDevice')}
                </h4>
              </TooltipTrigger>
              <TooltipContent>
                <p>{device.name || device.displayName || t('auth:devices.unknownDevice')}</p>
              </TooltipContent>
            </Tooltip>
            {device.isCurrent && (
              <Badge variant="default" className="text-xs">
                {t('auth:devices.current')}
              </Badge>
            )}
            <DeviceStatusBadge status={device.status} />
          </div>

          <div className="mt-1 text-sm text-muted-foreground space-y-0.5">
            {deviceInfo && <div>{deviceInfo}</div>}
            {locationDisplay && (
              <div className="flex items-center gap-1.5">
                <Globe className="h-3.5 w-3.5" />
                <span>{locationDisplay}</span>
              </div>
            )}
            <div className="flex items-center gap-1.5">
              <span>{t('auth:devices.info.lastUsed')}:</span>
              <RelativeTime date={device.lastUsedAt} />
            </div>
          </div>
        </div>
      </button>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="icon" disabled={isLoading}>
            {isLoading ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <MoreVertical className="h-4 w-4" />
            )}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {showRename && onRename && !isRevoked && (
            <DropdownMenuItem onClick={() => onRename(device)}>
              <Pencil className="h-4 w-4 mr-2" />
              {t('auth:devices.actions.rename')}
            </DropdownMenuItem>
          )}
          {showApprove && isPending && onApprove && (
            <DropdownMenuItem onClick={() => onApprove(device.id)}>
              <CheckCircle className="h-4 w-4 mr-2" />
              {t('auth:devices.actions.approve')}
            </DropdownMenuItem>
          )}
          {!device.isCurrent && !isRevoked && (
            <DropdownMenuItem
              onClick={() => onRevoke(device.id)}
              className="text-destructive focus:text-destructive"
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('auth:devices.actions.revoke')}
            </DropdownMenuItem>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
