import { useTranslation } from 'react-i18next'
import {
  Monitor,
  Smartphone,
  Tablet,
  Globe,
  Clock,
  Shield,
  Edit,
  Trash2,
  Check,
} from 'lucide-react'

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { RelativeTime } from '@/components/shared'
import { DeviceStatusBadge } from './device-status-badge'
import { normalizeDeviceStatus, type DeviceDto } from '../types/device'

interface DeviceDetailsSheetProps {
  device: DeviceDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onRename?: (device: DeviceDto) => void
  onRevoke?: (device: DeviceDto) => void
  onApprove?: (device: DeviceDto) => void
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

export function DeviceDetailsSheet({
  device,
  open,
  onOpenChange,
  onRename,
  onRevoke,
  onApprove,
}: DeviceDetailsSheetProps) {
  const { t } = useTranslation()

  if (!device) return null

  const normalizedStatus = normalizeDeviceStatus(device.status)
  const browserInfo = [device.browser, device.browserVersion]
    .filter(Boolean)
    .join(' ')
  const osInfo = [device.operatingSystem, device.osVersion]
    .filter(Boolean)
    .join(' ')
  const locationDisplay =
    device.locationDisplay ||
    [device.city, device.country].filter(Boolean).join(', ')

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-md flex flex-col p-0">
        <SheetHeader className="sr-only">
          <SheetTitle>{t('auth:devices.details')}</SheetTitle>
          <SheetDescription>{t('auth:devices.title')}</SheetDescription>
        </SheetHeader>

        {/* Fixed top section */}
        <div className="space-y-6 p-6 pb-0 shrink-0">
          {/* Device header */}
          <div className="flex items-start gap-4">
            <div className="p-3 rounded-full bg-muted">
              <DeviceIcon
                deviceType={device.deviceType}
                className="h-6 w-6 text-muted-foreground"
              />
            </div>
            <div className="flex-1 min-w-0 space-y-1">
              <h2 className="text-xl font-semibold truncate">
                {device.name || device.displayName || t('auth:devices.unknownDevice')}
              </h2>
              <div className="flex items-center gap-2">
                <DeviceStatusBadge status={device.status} />
                {device.isCurrent && (
                  <Badge variant="default" className="text-xs">
                    {t('auth:devices.current')}
                  </Badge>
                )}
              </div>
            </div>
          </div>

          {/* Browser & OS Info */}
          <div className="space-y-4 rounded-lg border p-4">
            {browserInfo && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Globe className="h-4 w-4" />
                  {t('auth:devices.info.browser')}
                </div>
                <div className="text-sm font-medium">{browserInfo}</div>
              </div>
            )}
            {osInfo && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Monitor className="h-4 w-4" />
                  {t('auth:devices.info.os')}
                </div>
                <div className="text-sm font-medium">{osInfo}</div>
              </div>
            )}
            {device.deviceType && (
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Smartphone className="h-4 w-4" />
                  {t('common:labels.type')}
                </div>
                <div className="text-sm font-medium">{device.deviceType}</div>
              </div>
            )}
          </div>
        </div>

        {/* Scrollable content section */}
        <div className="flex-1 overflow-y-auto px-6 py-4 min-h-0">
          {/* Location */}
          {(locationDisplay || device.ipAddress) && (
            <div className="space-y-3 mb-6">
              <div className="flex items-center gap-2">
                <Globe className="h-4 w-4 text-muted-foreground" />
                <h3 className="text-sm font-medium">{t('auth:devices.info.location')}</h3>
              </div>
              <div className="rounded-lg border p-4 space-y-4">
                {locationDisplay && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">{t('auth:devices.info.location')}</span>
                    <span className="text-sm font-medium">{locationDisplay}</span>
                  </div>
                )}
                {device.ipAddress && (
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">IP</span>
                    <code className="text-xs bg-muted px-2 py-1 rounded">
                      {device.ipAddress}
                    </code>
                  </div>
                )}
              </div>
            </div>
          )}

          <Separator className="my-6" />

          {/* Activity */}
          <div className="space-y-3">
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-muted-foreground" />
              <h3 className="text-sm font-medium">{t('common:labels.activity')}</h3>
            </div>
            <div className="rounded-lg border p-4 space-y-4">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">{t('auth:devices.info.lastUsed')}</span>
                <span className="text-sm font-medium">
                  <RelativeTime date={device.lastUsedAt} />
                </span>
              </div>
              {normalizedStatus === 'Trusted' && device.trustedAt && (
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">{t('auth:devices.info.trustedSince')}</span>
                  <span className="text-sm font-medium">
                    <RelativeTime date={device.trustedAt} />
                  </span>
                </div>
              )}
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">{t('common:labels.created')}</span>
                <span className="text-sm font-medium">
                  <RelativeTime date={device.createdAt} />
                </span>
              </div>
            </div>
          </div>

          {/* Risk Score */}
          {device.riskScore !== null && device.riskScore !== undefined && (
            <>
              <Separator className="my-6" />
              <div className="space-y-3">
                <div className="flex items-center gap-2">
                  <Shield className="h-4 w-4 text-muted-foreground" />
                  <h3 className="text-sm font-medium">{t('common:labels.security')}</h3>
                </div>
                <div className="rounded-lg border p-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">
                      {t('common:labels.riskScore')}
                    </span>
                    <Badge
                      variant={
                        device.riskScore < 30
                          ? 'default'
                          : device.riskScore < 60
                          ? 'secondary'
                          : 'destructive'
                      }
                    >
                      {device.riskScore}
                    </Badge>
                  </div>
                </div>
              </div>
            </>
          )}

          {/* Device ID */}
          <div className="mt-6 text-xs text-muted-foreground">
            <span className="font-medium">Device ID:</span>{' '}
            <code className="bg-muted px-1 py-0.5 rounded break-all">
              {device.deviceId}
            </code>
          </div>
        </div>

        {/* Fixed bottom actions */}
        {!device.isCurrent && (onRename || onRevoke || onApprove) && (
          <div className="shrink-0 border-t p-6 flex flex-col gap-2">
            {onApprove && normalizedStatus === 'PendingApproval' && (
              <Button
                variant="outline"
                className="w-full"
                onClick={() => onApprove(device)}
              >
                <Check className="mr-2 h-4 w-4" />
                {t('auth:devices.actions.approve')}
              </Button>
            )}
            {onRename && (
              <Button
                variant="outline"
                className="w-full"
                onClick={() => onRename(device)}
              >
                <Edit className="mr-2 h-4 w-4" />
                {t('common:actions.rename')}
              </Button>
            )}
            {onRevoke && normalizedStatus !== 'Revoked' && (
              <Button
                variant="outline"
                className="w-full"
                onClick={() => onRevoke(device)}
              >
                <Trash2 className="mr-2 h-4 w-4" />
                {t('auth:devices.actions.revoke')}
              </Button>
            )}
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
